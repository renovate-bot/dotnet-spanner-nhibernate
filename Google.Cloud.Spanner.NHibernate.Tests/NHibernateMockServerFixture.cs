// Copyright 2021 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Cloud.Spanner.Connection.Tests.MockServer;
using Google.Cloud.Spanner.NHibernate.Tests.Entities;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NHibernate.Util;
using Environment = NHibernate.Cfg.Environment;
using PropertyGeneration = NHibernate.Mapping.PropertyGeneration;

namespace Google.Cloud.Spanner.NHibernate.Tests
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class NHibernateMockServerFixture : SpannerMockServerFixture
    {
        public NHibernateMockServerFixture()
        {
            ReflectHelper.ClassForName(typeof(SpannerDriver).AssemblyQualifiedName);
            Configuration = new Configuration().DataBaseIntegration(db =>
            {
                db.Dialect<SpannerDialect>();
                db.ConnectionString = ConnectionString;
                db.ConnectionProvider<TestConnectionProvider>();
            });
            var mapper = new ModelMapper();
            mapper.AddMapping<SingerMapping>();
            mapper.AddMapping<AlbumMapping>();
            mapper.AddMapping<TableWithAllColumnTypesMapping>();
            mapper.AddMapping<TrackMapping>();
            mapper.AddMapping<SingerWithVersionMapping>();
            mapper.AddMapping<AlbumWithVersionMapping>();
            
            mapper.AddMapping<PersonMapping>();
            mapper.AddMapping<StudentMapping>();
            mapper.AddMapping<TeacherMapping>();
            
            mapper.AddMapping<InvoiceMapping>();
            mapper.AddMapping<InvoiceLineMapping>();
            mapper.AddMapping<InvoiceLineNoteMapping>();
            
            mapper.AddMapping<BandMapping>();
            
            var mapping = mapper.CompileMappingForAllExplicitlyAddedEntities();
            Configuration.AddMapping(mapping);
            
            SessionFactory = Configuration.BuildSessionFactory();
                        
            // This is needed for support for query hints.
            Configuration.SetInterceptor(new SpannerQueryHintInterceptor());
            Configuration.Properties[Environment.UseSqlComments] = "true";
            SessionFactoryWithComments = Configuration.BuildSessionFactory();

            // Configure some entities to use mutations instead of DML in a separate SessionFactory.
            Configuration.GetClassMapping(typeof(Singer)).EntityPersisterClass = typeof(SpannerSingleTableEntityPersister);
            Configuration.GetClassMapping(typeof(Singer)).DynamicUpdate = true;
            Configuration.GetClassMapping(typeof(Album)).EntityPersisterClass = typeof(SpannerSingleTableEntityPersister);
            Configuration.GetClassMapping(typeof(Album)).DynamicUpdate = true;
            Configuration.GetClassMapping(typeof(Track)).EntityPersisterClass = typeof(SpannerSingleTableEntityPersister);
            Configuration.GetClassMapping(typeof(Track)).DynamicUpdate = true;
            Configuration.GetClassMapping(typeof(Band)).EntityPersisterClass = typeof(SpannerSingleTableEntityPersister);
            Configuration.GetClassMapping(typeof(Band)).DynamicUpdate = true;
            Configuration.GetClassMapping(typeof(SingerWithVersion)).EntityPersisterClass = typeof(SpannerSingleTableEntityPersister);
            Configuration.GetClassMapping(typeof(SingerWithVersion)).DynamicUpdate = true;
            Configuration.GetClassMapping(typeof(AlbumWithVersion)).EntityPersisterClass = typeof(SpannerSingleTableEntityPersister);
            Configuration.GetClassMapping(typeof(AlbumWithVersion)).DynamicUpdate = true;
            Configuration.GetClassMapping(typeof(TableWithAllColumnTypes)).DynamicUpdate = true;
            // Disable property generation when we are using mutations, as the value cannot be read before everything
            // has been committed. This means that the value will still be generated by the database, but it will not
            // be read back automatically by NHibernate, and the user needs to manually reload it from the database to
            // get the value.
            Configuration.GetClassMapping(typeof(Singer)).GetProperty(nameof(Singer.FullName)).Generation = PropertyGeneration.Never;
            Configuration.GetClassMapping(typeof(TableWithAllColumnTypes)).GetProperty(nameof(TableWithAllColumnTypes.ColComputed)).Generation = PropertyGeneration.Never;
            Configuration.GetClassMapping(typeof(Person)).GetProperty(nameof(Person.FullName)).Generation = PropertyGeneration.Never;
            // This is needed to be able to use mutations with versioned data. Otherwise, NHibernate will never use
            // batching for versioned data, and mutations are only supported during batches.
            Configuration.Properties[Environment.BatchVersionedData] = "true";
            SessionFactoryUsingMutations = Configuration.BuildSessionFactory();
        }

        public Configuration Configuration { get; }

        public ISessionFactory SessionFactory { get; }
        
        public ISessionFactory SessionFactoryWithComments { get; }
        
        public ISessionFactory SessionFactoryUsingMutations { get; }
        
        public string ConnectionString => $"Data Source=projects/p1/instances/i1/databases/d1;Host={Host};Port={Port}";
    }
}