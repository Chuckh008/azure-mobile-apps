﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Zumo.Server.Test.E2EServer.DataObjects;
using Microsoft.Zumo.Server.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Movie = Microsoft.Zumo.Server.Test.E2EServer.DataObjects.Movie;

namespace Microsoft.Zumo.Server.Test.TableController
{
    [TestClass]
    public class List_Tests : Base_Test
    {
        [TestMethod]
        public async Task GetItems_ReturnsSomeItems()
        {
            var response = await SendRequestToServer<Movie>(HttpMethod.Get, "/tables/movies?$count=true", null);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var actual = await GetValueFromResponse<PagedList<Movie>>(response);
            Assert.IsNotNull(actual);
            CollectionAssert.AllItemsAreNotNull(actual.Values);
            CollectionAssert.AllItemsAreUnique(actual.Values);
            Assert.AreEqual(50, actual.Values.Count);
            Assert.IsNotNull(actual.NextLink);
            Assert.IsNotNull(actual.Count);
            Assert.AreEqual(248, actual.Count);
        }

        [TestMethod]
        public async Task GetItems_WithFilter_ReturnsSomeItems()
        {
            var response = await SendRequestToServer<Movie>(HttpMethod.Get, "/tables/movies?$filter=mpaaRating eq 'R'&$count=true", null);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var actual = await GetValueFromResponse<PagedList<Movie>>(response);
            Assert.IsNotNull(actual);
            CollectionAssert.AllItemsAreNotNull(actual.Values);
            CollectionAssert.AllItemsAreUnique(actual.Values);
            Assert.AreEqual(50, actual.Values.Count);
            Assert.IsNotNull(actual.NextLink);
            Assert.IsNotNull(actual.Count);
            Assert.AreEqual(94, actual.Count);
            Assert.IsNotNull(actual.MaxTop);
            Assert.IsNotNull(actual.PageSize);
        }

        [TestMethod]
        public async Task GetItems_ExcludingItems_ReturnsSomeItems()
        {
            var response = await SendRequestToServer<Movie>(HttpMethod.Get, "/tables/movies?$filter=mpaaRating eq 'R'&$count=true&__excludeitems=true", null);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var actual = await GetValueFromResponse<PagedList<Movie>>(response);
            Assert.IsNotNull(actual);
            Assert.IsNull(actual.Values);
            Assert.IsNull(actual.NextLink);
            Assert.IsNotNull(actual.Count);
            Assert.AreEqual(94, actual.Count);
            Assert.IsNotNull(actual.MaxTop);
            Assert.IsTrue(actual.MaxTop > 0);
            Assert.IsNotNull(actual.PageSize);
            Assert.IsTrue(actual.PageSize > 0);
        }

        [TestMethod]
        public async Task GetItems_InvalidOData_Returns400()
        {
            var response = await SendRequestToServer<Movie>(HttpMethod.Get, "/tables/movies?$filter=invalid", null);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task GetItems_SoftDelete_DoesNotIncludeDeletedItems()
        {
            var deleteResponse = await SendRequestToServer<SUnit>(HttpMethod.Delete, "/tables/sunits/sunit-14", null);
            Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var response = await SendRequestToServer<SUnit>(HttpMethod.Get, "/tables/sunits", null);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var actual = await GetValueFromResponse<PagedList<SUnit>>(response);

            Assert.IsFalse(actual.Values.Where(item => item.Deleted == true).Any());
        }

        [TestMethod]
        public async Task GetItems_SoftDelete_CanIncludeDeletedItems()
        {
            var deleteResponse = await SendRequestToServer<SUnit>(HttpMethod.Delete, "/tables/sunits/sunit-2", null);
            var firstResponse = await SendRequestToServer<SUnit>(HttpMethod.Get, "/tables/sunits", null);
            Assert.AreEqual(HttpStatusCode.OK, firstResponse.StatusCode);
            var firstActual = await GetValueFromResponse<PagedList<SUnit>>(firstResponse);

            var secondResponse = await SendRequestToServer<SUnit>(HttpMethod.Get, "/tables/sunits?__includedeleted=true&$top=500", null);
            Assert.AreEqual(HttpStatusCode.OK, secondResponse.StatusCode);
            var secondActual = await GetValueFromResponse<PagedList<SUnit>>(secondResponse);

            //Assert.IsTrue(secondActual.Count > firstActual.Count);
            Assert.IsTrue(secondActual.Values.Where(item => item.Deleted == true).Any());
        }

        [TestMethod]
        public async Task GetItems_Unauthorzed_Returns403()
        {
            var response = await SendRequestToServer<Movie>(HttpMethod.Get, "/tables/unauthorized", null);
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task GetItems_MaxTop_ReturnsNItems()
        {
            var response = await SendRequestToServer<Movie>(HttpMethod.Get, "/tables/movies?$top=5", null);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var actual = await GetValueFromResponse<PagedList<Movie>>(response);
            Assert.IsNotNull(actual);
            CollectionAssert.AllItemsAreNotNull(actual.Values);
            CollectionAssert.AllItemsAreUnique(actual.Values);
            Assert.AreEqual(5, actual.Values.Count);
        }
    }
}