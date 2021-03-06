﻿using System;
using System.ComponentModel.Composition.Hosting;
using Raven.Abstractions;
using Raven.Bundles.CreatedDate;
using Raven.Client.Contrib.Tests.TestEntities;
using Raven.Database.Config;
using Raven.Tests.Helpers;
using Xunit;

namespace Raven.Bundles.Contrib.Tests.CreatedDate
{
    public class CreatedDateTests : RavenTestBase
    {
        protected override void ModifyConfiguration(RavenConfiguration configuration)
        {
            // Wire up the bundle to the embedded database
            configuration.Catalog.Catalogs.Add(new AssemblyCatalog(typeof(CreatedDateTrigger).Assembly));
        }

        [Fact]
        public void CreatedDateBundle_Sets_Created_Metadata_On_First_Write()
        {
            using (var documentStore = NewDocumentStore())
            {
                var utcNow = SystemTime.UtcNow;

                using (var session = documentStore.OpenSession())
                {
                    session.Store(new Ball { Id = "balls/1", Color = "Red" });
                    session.SaveChanges();
                }

                using (var session = documentStore.OpenSession())
                {
                    var ball = session.Load<Ball>("balls/1");
                    var metadata = session.Advanced.GetMetadataFor(ball);
                    var created = metadata.Value<DateTime?>("Created");

                    Assert.NotNull(created);
                    if (created == null) return;
                    Assert.InRange(created.Value.ToUniversalTime(), utcNow, utcNow.AddSeconds(1));
                }
            }
        }

        [Fact]
        public void CreatedDateBundle_Does_Not_Change_Created_Metadata_On_Update()
        {
            using (var documentStore = NewDocumentStore())
            {
                using (var session = documentStore.OpenSession())
                {
                    session.Store(new Ball { Id = "balls/1", Color = "Red" });
                    session.SaveChanges();
                }

                using (var session = documentStore.OpenSession())
                {
                    var ball = session.Load<Ball>("balls/1");
                    ball.Color = "Blue";
                    session.SaveChanges();
                }

                using (var session = documentStore.OpenSession())
                {
                    var ball = session.Load<Ball>("balls/1");
                    var metadata = session.Advanced.GetMetadataFor(ball);
                    var created = metadata.Value<DateTime>("Created").ToUniversalTime();
                    var modified = metadata.Value<DateTime>("Last-Modified").ToUniversalTime();

                    Assert.NotEqual(modified, created);
                    Assert.True(modified > created);
                }
            }
        }

        [Fact]
        public void CreatedDateBundle_Created_Metadata_Equals_First_Modified_Metadata()
        {
            using (var documentStore = NewDocumentStore())
            {
                using (var session = documentStore.OpenSession())
                {
                    session.Store(new Ball { Id = "balls/1", Color = "Red" });
                    session.SaveChanges();
                }

                using (var session = documentStore.OpenSession())
                {
                    var ball = session.Load<Ball>("balls/1");
                    var metadata = session.Advanced.GetMetadataFor(ball);
                    var created = metadata.Value<DateTime>("Created").ToUniversalTime();
                    var modified = metadata.Value<DateTime>("Last-Modified").ToUniversalTime();

                    Assert.Equal(modified, created);
                }
            }
        }

        [Fact]
        public void CreatedDateBundle_LoadTest()
        {
            using (var documentStore = NewDocumentStore())
            {
                using (var session = documentStore.OpenSession())
                {
                    for (int i = 0; i < 1000; i++)
                        session.Store(Ball.Random);

                    session.SaveChanges();
                }
            }
        }
    }
}
