﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Albireo.Data;
using StarryEyes.Models.Store;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models.Tab
{
    public class TimelineModel : IDisposable
    {
        public readonly int TimelineChunkCount = 250;
        public readonly int TimelineChunkCountBounce = 50;

        private object _sicLocker = new object();
        private AVLTree<long> _statusIdCache;
        private Func<TwitterStatus, bool> _evaluator;
        private Func<long?, int, IObservable<TwitterStatus>> _fetcher;
        private CompositeDisposable _disposable;

        public TimelineModel(Func<TwitterStatus, bool> evaluator,
            Func<long?, int, IObservable<TwitterStatus>> fetcher)
        {
            this._evaluator = evaluator;
            this._fetcher = fetcher;
            this._statusIdCache = new AVLTree<long>();
            this._disposable = new CompositeDisposable();

            // listen status stream
            _disposable.Add(StatusStore.StatusPublisher
                .Where(sn => !sn.IsAdded || evaluator(sn.Status))
                .Subscribe(AcceptStatusNotification));
        }

        private void AcceptStatusNotification(StatusNotification notification)
        {
            if (notification.IsAdded)
                AddStatus(notification.Status);
            else
                RemoveStatus(notification.StatusId);
        }

        private void AddStatus(TwitterStatus status)
        {
            bool add = false;
            lock (_sicLocker)
            {
                add = _statusIdCache.AddDistinct(status.Id);
            }
            if (add)
            {
                // add
                _statuses.Insert(
                    i => i.TakeWhile(_ => _.CreatedAt > status.CreatedAt).Count(),
                    status);
                if (_statusIdCache.Count > TimelineChunkCount + TimelineChunkCountBounce)
                    TrimTimeline();
            }
        }

        private void RemoveStatus(long id)
        {
            bool remove = false;
            lock (_sicLocker)
            {
                remove = _statusIdCache.Remove(id);
            }
            if (remove)
            {
                // remove
                _statuses.RemoveWhere(s => s.Id == id);
            }
        }

        private readonly ObservableSynchronizedCollectionEx<TwitterStatus> _statuses
            = new ObservableSynchronizedCollectionEx<TwitterStatus>();
        public ObservableSynchronizedCollectionEx<TwitterStatus> Statuses
        {
            get { return _statuses; }
        }

        public IObservable<Unit> ReadMore(long? maxId)
        {
            return Observable.Defer(() => this._fetcher(maxId, TimelineChunkCount))
                .Do(this.AddStatus)
                .OfType<Unit>();
        }

        private bool _isSuppressTimelineTrimming = false;
        public bool IsSuppressTimelineTrimming
        {
            get { return _isSuppressTimelineTrimming; }
            set
            {
                if (_isSuppressTimelineTrimming != value)
                {
                    _isSuppressTimelineTrimming = value;
                    if (!value)
                        TrimTimeline();
                }
            }
        }

        private void TrimTimeline()
        {
            if (_isSuppressTimelineTrimming) return;
            if (_statuses.Count > TimelineChunkCount) return;
            var lastCreatedAt = _statuses[TimelineChunkCount].CreatedAt;
            List<long> removedIds = new List<long>();
            _statuses.RemoveWhere(t =>
            {
                if (t.CreatedAt < lastCreatedAt)
                {
                    removedIds.Add(t.Id);
                    return true;
                }
                else
                {
                    return false;
                }
            });
            lock (_sicLocker)
            {
                removedIds.ForEach(i => _statusIdCache.Remove(i));
            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
            _statusIdCache.Clear();
            _statuses.Clear();
        }
    }
}