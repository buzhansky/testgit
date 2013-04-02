using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using ComfortSleep.Common.Base;
using ComfortSleep.Common.Base.Extensions;
using ComfortSleep.Common.DataAccess;
using ComfortSleep.Domain;

namespace ComfortSleep.Services.BusinessLogic.Services
{
    public class ComplianceLevelService : Service<ComplianceLevel,int>
    {
        private readonly string connection;
        public ComplianceLevelService(DbContextProviderFactory dbContextFactory, string connection) : base(dbContextFactory)
        {
            this.connection = connection;
        }

        public override List<ComplianceLevel> List(Expression<Func<ComplianceLevel, bool>> predicate = null, Expression<Func<ComplianceLevel, object>>[] prefetches = null, string sortExpression = null, int pageIndex = 0, int pageSize = 2147483647)
        {
            prefetches = new Expression<Func<ComplianceLevel, object>>[] { item => item.ComplianceTerm };
            var items = base.List(predicate, prefetches, sortExpression, pageIndex, pageSize);
            items.Do(its => its.ForEach(ClearLevelInItems));

            return items;
        }

        public override int ListCount(Expression<Func<ComplianceLevel, bool>> predicate = null, Expression<Func<ComplianceLevel, object>>[] prefetches = null)
        {
            prefetches = new Expression<Func<ComplianceLevel, object>>[] {item => item.ComplianceTerm};
            return base.ListCount(predicate, prefetches);
        }

        public override ComplianceLevel GetById(int id, params Expression<Func<ComplianceLevel, object>>[] prefetches)
        {
            prefetches = new Expression<Func<ComplianceLevel, object>>[] { item => item.Questions, item => item.ComplianceTerm};

            var entity = base.GetById(id, prefetches);
            entity.Do(ClearLevelInItems);

            return entity;
        }

        public override ComplianceLevel GetByExpression(Expression<Func<ComplianceLevel, bool>> predicate, params Expression<Func<ComplianceLevel, object>>[] prefetches)
        {
            prefetches = new Expression<Func<ComplianceLevel, object>>[] { item => item.Questions };
            var entity = base.GetByExpression(predicate, prefetches);
            entity.Do(ClearLevelInItems);

            return entity;
        }

        public ComplianceLevel GetNextLevel(ComplianceLevel level)
        {
            if(level == null)
                throw new ArgumentNullException("level");

            var entity = base.GetById(level.Id);
            return GetByExpression(item => item.TermId == entity.TermId && item.Id > level.Id);
        }

        public override ComplianceLevel Save(ComplianceLevel entity)
        {
            if(entity == null)
                throw new ArgumentNullException("entity");

            using (var transaction = new TransactionScope())
            {
                entity = SaveWithoutTransaction(entity);

                transaction.Complete();

                return entity;
            }
        }

        public override List<ComplianceLevel> Save(List<ComplianceLevel> entities)
        {
            if(entities == null)
                throw new ArgumentNullException();

            var items = new List<ComplianceLevel>();
            using (var transaction = new TransactionScope())
            {
                entities.ForEach(item => items.Add(SaveWithoutTransaction(item)));
                transaction.Complete();

                return items;
            }
        }

        internal ComplianceLevel SaveWithoutTransaction(ComplianceLevel entity)
        {
              if(entity == null)
                  throw new ArgumentNullException("entity");

            var questions = entity.Questions;
            questions.ForEach(item =>
                {
                    item.ComplianceLevel = null;
                    item.ComplianceQuestion = null;
                });

            entity.ComplianceTerm = null;
            entity.Questions = null;
            entity = base.Save(entity);

            SaveRelatedEntitites<ComplianceLevelToQuestion, int, int>(entity.Id, item => item.LevelId, questions);

            ClearLevelInItems(entity);

            return entity;
        }

        private void ClearLevelInItems(ComplianceLevel entity)
        {
            if (entity.ComplianceTerm != null)
                entity.ComplianceTerm.ComplianceLevels = null;

            if (entity.Questions != null)
                entity.Questions.ForEach(item => item.ComplianceLevel = null);
        }
    }
}
