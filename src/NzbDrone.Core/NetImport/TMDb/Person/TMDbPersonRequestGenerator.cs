using NzbDrone.Common.Http;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.NetImport.TMDb.Person
{
    public class TMDbPersonRequestGenerator : INetImportRequestGenerator
    {
        public TMDbPersonSettings Settings { get; set; }
        public IHttpClient HttpClient { get; set; }
        public IHttpRequestBuilderFactory RequestBuilder { get; set; }
        public Logger Logger { get; set; }

        public TMDbPersonRequestGenerator()
        {
        }

        public virtual NetImportPageableRequestChain GetMovies()
        {
            var pageableRequests = new NetImportPageableRequestChain();

            pageableRequests.Add(GetMoviesRequest());

            return pageableRequests;
        }

        private IEnumerable<NetImportRequest> GetMoviesRequest()
        {
            Logger.Info($"Importing TMDb movies from person: {Settings.PersonId}");

            var minVoteCount = Settings.FilterCriteria.MinVotes;
            var minVoteAverage = Settings.FilterCriteria.MinVoteAverage;
            var ceritification = Settings.FilterCriteria.Ceritification;
            var includeGenreIds = Settings.FilterCriteria.IncludeGenreIds;
            var excludeGenreIds = Settings.FilterCriteria.ExcludeGenreIds;
            var languageCode = (TMDbLanguageCodes)Settings.FilterCriteria.LanguageCode;

            if (ceritification.IsNotNullOrWhiteSpace())
            {
                ceritification = $"&certification_country=US&certification={ceritification}";
            }

            yield return new NetImportRequest(RequestBuilder.Create()
                                                            .SetSegment("route", "collection")
                                                            .SetSegment("id", Settings.PersonId)
                                                            .SetSegment("secondaryRoute", "")
                                                            .AddQueryParam("vote_count.gte", minVoteCount)
                                                            .AddQueryParam("vote_average.gte", minVoteAverage)
                                                            .AddQueryParam("with_genres", includeGenreIds)
                                                            .AddQueryParam("without_genres", excludeGenreIds)
                                                            .AddQueryParam("certification_country", "US")
                                                            .AddQueryParam("certification", ceritification)
                                                            .AddQueryParam("with_original_language", languageCode)
                                                            .Accept(HttpAccept.Json)
                                                            .Build());

        }
    }
}