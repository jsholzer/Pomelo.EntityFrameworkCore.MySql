﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;

namespace Pomelo.EntityFrameworkCore.MySql.FunctionalTests.Query
{
    public class NorthwindIncludeNoTrackingQueryMySqlTest : NorthwindIncludeNoTrackingQueryTestBase<NorthwindQueryMySqlFixture<NoopModelCustomizer>>
    {
        // ReSharper disable once UnusedParameter.Local
        public NorthwindIncludeNoTrackingQueryMySqlTest(NorthwindQueryMySqlFixture<NoopModelCustomizer> fixture)
            : base(fixture)
        {
            ClearLog();
        }

        public override async Task Include_collection_with_last_no_orderby(bool async)
            => Assert.Equal(
                RelationalStrings.LastUsedWithoutOrderBy(nameof(Enumerable.Last)),
                (await Assert.ThrowsAsync<InvalidOperationException>(
                    () => base.Include_collection_with_last_no_orderby(async))).Message);

        public override Task Include_duplicate_collection_result_operator2(bool async)
        {
            // The order of `Orders` can be different, because it is not explicitly sorted.
            // The order of the end result can be different as well.
            // This is the case on MariaDB.
            return AssertQuery(
                async,
                ss => (from c1 in ss.Set<Customer>().Include(c => c.Orders).OrderBy(c => c.CustomerID).ThenBy(c => c.Orders.FirstOrDefault() != null ? c.Orders.FirstOrDefault().CustomerID : null).Take(2)
                    from c2 in ss.Set<Customer>().OrderBy(c => c.CustomerID).Skip(2).Take(2)
                    select new { c1, c2 }).OrderBy(t => t.c1.CustomerID).ThenBy(t => t.c2.CustomerID).Take(1),
                elementSorter: e => (e.c1.CustomerID, e.c2.CustomerID),
                elementAsserter: (e, a) =>
                {
                    AssertInclude(e.c1, a.c1, new ExpectedInclude<Customer>(c => c.Orders));
                    AssertEqual(e.c2, a.c2);
                },
                entryCount: 8);
        }

        public override Task Repro9735(bool async)
        {
            return AssertQuery(
                async,
                ss => ss.Set<Order>()
                    .Include(b => b.OrderDetails)
                    .OrderBy(b => b.Customer.CustomerID != null)
                    .ThenBy(b => b.Customer != null ? b.Customer.CustomerID : string.Empty)
                    .ThenBy(b => b.EmployeeID) // Needs to be explicitly ordered by EmployeeID as well
                    .Take(2),
                entryCount: 6);
        }

        public override Task Include_duplicate_collection_result_operator(bool async)
        {
            // The order of `Orders` can be different, because it is not explicitly sorted.
            // The order of the end result can be different as well.
            // This is the case on MariaDB.
            return AssertQuery(
                async,
                ss => (from c1 in ss.Set<Customer>().Include(c => c.Orders).OrderBy(c => c.CustomerID).ThenBy(c => c.Orders.FirstOrDefault() != null ? c.Orders.FirstOrDefault().CustomerID : null).Take(2)
                    from c2 in ss.Set<Customer>().Include(c => c.Orders).OrderBy(c => c.CustomerID).Skip(2).Take(2)
                    select new { c1, c2 }).OrderBy(t => t.c1.CustomerID).ThenBy(t => t.c2.CustomerID).Take(1),
                elementSorter: e => (e.c1.CustomerID, e.c2.CustomerID),
                elementAsserter: (e, a) =>
                {
                    AssertInclude(e.c1, a.c1, new ExpectedInclude<Customer>(c => c.Orders));
                    AssertInclude(e.c2, a.c2, new ExpectedInclude<Customer>(c => c.Orders));
                },
                entryCount: 15);
        }

        public override Task Include_collection_with_multiple_conditional_order_by(bool async)
        {
            return AssertQuery(
                async,
                ss => ss.Set<Order>()
                    .Include(c => c.OrderDetails)
                    .OrderBy(o => o.OrderID > 0)
                    .ThenBy(o => o.Customer != null ? o.Customer.City : string.Empty)
                    .ThenBy(b => b.EmployeeID) // Needs to be explicitly ordered by EmployeeID as well
                    .Take(5),
                elementAsserter: (e, a) => AssertInclude(e, a, new ExpectedInclude<Order>(o => o.OrderDetails)),
                entryCount: 14);
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        protected override void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();
    }
}
