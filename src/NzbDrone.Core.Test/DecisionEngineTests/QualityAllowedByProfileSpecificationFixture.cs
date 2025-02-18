using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class QualityAllowedByProfileSpecificationFixture : CoreTest<QualityAllowedByProfileSpecification>
    {
        private RemoteMovie remoteMovie;

        public static object[] AllowedTestCases =
        {
            new object[] { Quality.DVD },
            new object[] { Quality.HDTV720p },
            new object[] { Quality.Bluray1080p }
        };

        public static object[] DeniedTestCases =
        {
            new object[] { Quality.SDTV },
            new object[] { Quality.WEBDL720p },
            new object[] { Quality.Bluray720p }
        };

        [SetUp]
        public void Setup()
        {
            var fakeSeries = Builder<Movie>.CreateNew()
                .With(c => c.Profile = new Profile { Cutoff = Quality.Bluray1080p.Id })
                         .Build();

            remoteMovie = new RemoteMovie
            {
                Movie = fakeSeries,
                ParsedMovieInfo = new ParsedMovieInfo { Quality = new QualityModel(Quality.DVD, new Revision(version: 2)) },
            };
        }

        [Test, TestCaseSource("AllowedTestCases")]
        public void should_allow_if_quality_is_defined_in_profile(Quality qualityType)
        {
            remoteMovie.ParsedMovieInfo.Quality.Quality = qualityType;
            remoteMovie.Movie.Profile.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.DVD, Quality.HDTV720p, Quality.Bluray1080p);

            Subject.IsSatisfiedBy(remoteMovie, null).Accepted.Should().BeTrue();
        }

        [Test, TestCaseSource("DeniedTestCases")]
        public void should_not_allow_if_quality_is_not_defined_in_profile(Quality qualityType)
        {
            remoteMovie.ParsedMovieInfo.Quality.Quality = qualityType;
            remoteMovie.Movie.Profile.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.DVD, Quality.HDTV720p, Quality.Bluray1080p);

            Subject.IsSatisfiedBy(remoteMovie, null).Accepted.Should().BeFalse();
        }
    }
}
