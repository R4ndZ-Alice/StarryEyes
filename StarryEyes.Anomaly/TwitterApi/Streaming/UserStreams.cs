﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace StarryEyes.Anomaly.TwitterApi.Streaming
{
    public static class UserStreams
    {
        private const string EndpointUserStreams = "https://userstream.twitter.com/1.1/user.json";

        public static IObservable<string> ConnectUserStreams(
            this IOAuthCredential credential, IEnumerable<string> tracks,
            bool repliesAll = false, bool followingsActivity = false)
        {
            var filteredTracks = tracks != null
                                     ? tracks.Where(t => !String.IsNullOrEmpty(t))
                                             .Distinct()
                                             .JoinString(",")
                                     : null;
            var param = new Dictionary<string, object>
            {
                {"track", String.IsNullOrEmpty(filteredTracks) ? null : filteredTracks },
                {"replies", repliesAll ? "all" : null},
                {"include_followings_activity", followingsActivity ? "true" : null}
            }.ParametalizeForGet();
            return Observable.Create<string>((observer, cancel) => Task.Run(async () =>
            {
                try
                {
                    // using GZip cause receiving elements delayed.
                    var client = credential.CreateOAuthClient(useGZip: false);
                    // disable connection timeout due to streaming specification
                    client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                    var endpoint = EndpointUserStreams;
                    if (!String.IsNullOrEmpty(param))
                    {
                        endpoint += "?" + param;
                    }
                    using (var stream = await client.GetStreamAsync(endpoint))
                    using (var reader = new StreamReader(stream))
                    {
                        // reader.EndOfStream 
                        while (!cancel.IsCancellationRequested)
                        {
                            var readLine = reader.ReadLineAsync();
                            var delay = Task.Delay(TimeSpan.FromSeconds(ApiAccessProperties.StreamingTimeoutSec));
                            if (await Task.WhenAny(readLine, delay) == delay)
                            {
                                // timeout
                                System.Diagnostics.Debug.WriteLine("#USERSTREAM# TIMEOUT.");
                                break;
                            }
                            var line = readLine.Result;
                            if (line == null)
                            {
                                // connection closed
                                System.Diagnostics.Debug.WriteLine("#USERSTREAM# CONNECTION CLOSED.");
                                break;
                            }
                            if (!String.IsNullOrEmpty(line))
                            {
                                // successfully completed
                                observer.OnNext(line);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("#USERSTREAM# error detected: " + ex.Message);
                    observer.OnError(ex);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("#USERSTREAM# disconnection detected. (CANCELLATION REQUEST? " + cancel.IsCancellationRequested + ")");
                if (!cancel.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("#USERSTREAM# notify disconnection to upper layer.");
                    observer.OnCompleted();
                }
            }));
        }
    }
}
