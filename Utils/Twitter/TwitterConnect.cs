﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Tweetinvi;
using Tweetinvi.Models;
using Tweety.Models;
using TweetyCore.Models;
using TweetyCore.Utils.StringMatcher;

namespace TweetyCore.Utils.Twitter
{
    public class TwitterConnect : ITwitterConnect
    {
        private readonly TweetResult _tweetResults = new TweetResult
        {
            Query = new List<QueryCategory>()
        };
        private readonly QueryCategory _dinasKesehatan = new QueryCategory
        {
            Id = "dinas_kesehatan",
            Name = "Dinas Kesehatan",
            Num = 0,
            Tweet = new List<HasilTweet>()
        };
        private readonly QueryCategory _dinasBinamarga = new QueryCategory
        {
            Id = "dinas_binamarga",
            Name = "Dinas Binamarga",
            Num = 0,
            Tweet = new List<HasilTweet>()
        };
        private readonly QueryCategory _dinasPemuda = new QueryCategory
        {
            Id = "dinas_pemuda",
            Name = "Dinas Pemuda",
            Num = 0,
            Tweet = new List<HasilTweet>()
        };
        private readonly QueryCategory _dinasPendidikan = new QueryCategory
        {
            Id = "dinas_pendidikan",
            Name = "Dinas Pendidikan",
            Num = 0,
            Tweet = new List<HasilTweet>()
        };
        private readonly QueryCategory _dinasSosial = new QueryCategory
        {
            Id = "dinas_sosial",
            Name = "Dinas Sosial",
            Num = 0,
            Tweet = new List<HasilTweet>()
        };
        private readonly QueryCategory _others = new QueryCategory
        {
            Id = "no_category",
            Name = "No Category",
            Num = 0,
            Tweet = new List<HasilTweet>()
        };
        private bool[] _categorized;
        private readonly ILogger<TwitterConnect> _logger;
        private readonly IKMP _kmp;
        private readonly IBooyer _booyer;

        public TwitterConnect(ILogger<TwitterConnect> logger,
            IKMP kmp,
            IBooyer booyer
            )
        {
            _logger = logger;
            _kmp = kmp;
            _booyer = booyer;
            _tweetResults.Query.Add(_dinasKesehatan);
            _tweetResults.Query.Add(_dinasBinamarga);
            _tweetResults.Query.Add(_dinasPemuda);
            _tweetResults.Query.Add(_dinasPendidikan);
            _tweetResults.Query.Add(_dinasSosial);
            _tweetResults.Query.Add(_others);
            Connect();
        }

        public TwitterConnect()
        {
            _tweetResults.Query.Add(_dinasKesehatan);
            _tweetResults.Query.Add(_dinasBinamarga);
            _tweetResults.Query.Add(_dinasPemuda);
            _tweetResults.Query.Add(_dinasPendidikan);
            _tweetResults.Query.Add(_dinasSosial);
            _tweetResults.Query.Add(_others);
            Connect();
        }

        public TweetResponse ProcessTag(Tags tags)
        {
            int sumOfTweets = ParseTag(tags);
            return new TweetResponse()
            {
                Count = sumOfTweets,
                Data = _tweetResults
            };
        }

        #region Private Methods
        private void Connect()
        {
            string customer_key = Environment.GetEnvironmentVariable("CUSTOMER_KEY");
            string customer_secret = Environment.GetEnvironmentVariable("CUSTOMER_SECRET");
            string token = Environment.GetEnvironmentVariable("TOKEN");
            string token_secret = Environment.GetEnvironmentVariable("TOKEN_SECRET");
            // When a new thread is created, the default credentials will be the Application Credentials
            Auth.ApplicationCredentials = new TwitterCredentials(customer_key, customer_secret, token, token_secret);
            _logger.LogInformation("Auth Completed");
        }

        private int ParseTag(Tags tag)
        {
            int sumOfTweet = 0;
            var searchParameter = Search.CreateTweetSearchParameter(tag.Name);
            searchParameter.MaximumNumberOfResults = 100;
            var tweets = Search.SearchTweets(searchParameter);
            if (tweets != null)
            {
                sumOfTweet = tweets.Count();
                _categorized = new bool[sumOfTweet];
                for (int i = 0; i < sumOfTweet; i++)
                {
                    _categorized[i] = false;
                }

                foreach (QueryCategory category in _tweetResults.Query)
                {
                    string keywords = "";
                    if (category.Id == "dinas_kesehatan")
                    {
                        keywords = tag.DinasKesehatan;
                    }
                    else if (category.Id == "dinas_binamarga")
                    {
                        keywords = tag.DinasBinamarga;
                    }
                    else if (category.Id == "dinas_pendidikan")
                    {
                        keywords = tag.DinasPendidikan;
                    }
                    else if (category.Id == "dinas_pemuda")
                    {
                        keywords = tag.DinasPemuda;
                    }
                    else if (category.Id == "dinas_sosial")
                    {
                        keywords = tag.DinasSosial;
                    }

                    if (keywords != null && keywords != "")
                    {
                        GetQuery(category, tweets, keywords, tag.IsKMP);
                    }
                }
                for (int j = 0; j < sumOfTweet; j++)
                {
                    if (!_categorized[j])
                    {
                        HasilTweet hasilTemp = new HasilTweet
                        {
                            TweetContent = tweets.ElementAt(j),
                            Result = tweets.ElementAt(j).Text
                        };
                        _tweetResults.Query.Find(query => query.Id == "no_category").Tweet.Add(hasilTemp);
                    }
                }
            }
            return sumOfTweet;
        }

        private void GetQuery(QueryCategory category, IEnumerable<ITweet> tweets, string keywords, bool isKMP)
        {
            string[] keywordsArray = keywords.Split(",");
            int i = 0;
            foreach (ITweet tweet in tweets)
            {
                int indexFound;
                int cats = 0;
                string newText = tweet.Text;
                foreach (string keyWord in keywordsArray)
                {
                    indexFound = -1;
                    if (isKMP)
                    {
                        indexFound = _kmp.Solve(tweet.Text, keyWord);
                    }
                    else
                    {
                        indexFound = _booyer.Solve(tweet.Text, keyWord);
                    }
                    if (indexFound != -1)
                    {
                        cats++;
                        _categorized[i] = true;
                        newText = Regex.Replace(newText, keyWord, @"<b>$&</b>", RegexOptions.IgnoreCase);
                    }
                }
                if (cats > 0)
                {
                    HasilTweet hasil = new HasilTweet
                    {
                        TweetContent = tweet,
                        Result = newText
                    };
                    _tweetResults.Query.Find(q => q.Id == category.Id).Tweet.Add(hasil);
                }
                i++;
            }
        }
        #endregion
    }
}