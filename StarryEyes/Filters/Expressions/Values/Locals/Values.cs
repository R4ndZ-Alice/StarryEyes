﻿using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class LocalUser : ValueBase
    {
        private readonly UserExpressionBase _expression;

        public LocalUser(UserExpressionBase expression)
        {
            this._expression = expression;
            expression.ReapplyRequested += this.ExpressionReapplyRequested;
        }

        private void ExpressionReapplyRequested(RelationDataChangedInfo obj)
        {
            if (obj != null) return;
            System.Diagnostics.Debug.WriteLine("local user reapply");
            this.RaiseReapplyFilter();
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                if (_expression.UserId != -1)
                    yield return FilterExpressionType.Numeric;
                yield return FilterExpressionType.Set;
            }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            var cache = _expression.UserId;
            return _ => cache;
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var cache = _expression.Users;
            return _ => cache;
        }

        public override string GetNumericSqlQuery()
        {
            return _expression.UserIdSql;
        }

        public override string GetSetSqlQuery()
        {
            return _expression.UsersSql;
        }

        public override void BeginLifecycle()
        {
            _expression.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            _expression.EndLifecycle();
        }

        public override string ToQuery()
        {
            return _expression.ToQuery();
        }
    }

    public sealed class LocalUserFollowing : ValueBase
    {
        private readonly UserExpressionBase _expression;

        public LocalUserFollowing(UserExpressionBase expression)
        {
            this._expression = expression;
            expression.ReapplyRequested += this.ExpressionReapplyRequested;
        }

        private void ExpressionReapplyRequested(RelationDataChangedInfo obj)
        {
            if (obj == null || obj.Change == RelationDataChange.Following)
            {
                this.RaiseReapplyFilter();
            }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var cache = _expression.Followings;
            return _ => cache;
        }

        public override string GetSetSqlQuery()
        {
            return _expression.FollowingsSql;
        }

        public override void BeginLifecycle()
        {
            _expression.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            _expression.EndLifecycle();
        }

        public override string ToQuery()
        {
            return _expression.ToQuery() + ".following"; // "friends" also ok
        }
    }

    public sealed class LocalUserFollowers : ValueBase
    {
        private readonly UserExpressionBase _expression;

        public LocalUserFollowers(UserExpressionBase expression)
        {
            this._expression = expression;
            expression.ReapplyRequested += this.ExpressionReapplyRequested;
        }

        private void ExpressionReapplyRequested(RelationDataChangedInfo obj)
        {
            if (obj == null || obj.Change == RelationDataChange.Follower)
            {
                this.RaiseReapplyFilter();
            }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var cache = _expression.Followers;
            return _ => cache;
        }

        public override string GetSetSqlQuery()
        {
            return _expression.FollowersSql;
        }

        public override void BeginLifecycle()
        {
            _expression.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            _expression.EndLifecycle();
        }

        public override string ToQuery()
        {
            return _expression.ToQuery() + ".followers";
        }
    }

    public sealed class LocalUserBlockings : ValueBase
    {
        private readonly UserExpressionBase _expression;

        public LocalUserBlockings(UserExpressionBase expression)
        {
            this._expression = expression;
            expression.ReapplyRequested += this.ExpressionReapplyRequested;
        }

        private void ExpressionReapplyRequested(RelationDataChangedInfo obj)
        {
            if (obj == null || obj.Change == RelationDataChange.Blocking)
            {
                this.RaiseReapplyFilter();
            }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var cache = _expression.Blockings;
            return _ => cache;
        }

        public override string GetSetSqlQuery()
        {
            return _expression.BlockingsSql;
        }

        public override void BeginLifecycle()
        {
            _expression.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            _expression.EndLifecycle();
        }

        public override string ToQuery()
        {
            return _expression.ToQuery() + ".blockings";
        }
    }
}
