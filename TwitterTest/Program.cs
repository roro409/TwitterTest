using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using System.Diagnostics;
using System.Threading;
using TwitterTest.Properties;
using Newtonsoft.Json;
using System.IO;

namespace TwitterTest
{
    class Program
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
       
        public static int tweetCounter = 0;
        public static Dictionary<string, int> hashTags = new Dictionary<string, int>();

        static async Task Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            var userCredentials = new TwitterCredentials(Settings.Default.twitterConsumerKey, Settings.Default.twitterConsumerSecret, Settings.Default.twitterAccessToken, Settings.Default.twitterAccessTokenSecret);
            var userClient = new TwitterClient(userCredentials); 
            var sampleStream = userClient.Streams.CreateSampleStream();

            if (Settings.Default.onlyEnglish) 
            {
                sampleStream.AddLanguageFilter(LanguageFilter.English);
            }

            sampleStream.StallWarnings = true;

            var timer = new Timer((e) =>
            {
                runReports();
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var tweetList = new List<Tweetinvi.Events.TweetReceivedEventArgs>();
            
            sampleStream.TweetReceived += (sender, arguments) =>
            {
                tweetList.Add(arguments);
                tweetCounter++;

                if (tweetList.Count > 25 | stopwatch.ElapsedMilliseconds > 1000) 
                {
                    processTweetList(tweetList);
                    tweetList.Clear();
                    stopwatch.Restart();
                }
            };

            StreamStart:

            var taskStream = sampleStream.StartAsync();

            try
            {
                await Task.WhenAll(taskStream);
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("timed out"))
                {
                    stopwatch.Restart();
                    while (stopwatch.ElapsedMilliseconds < 2000)
                    {
                        Thread.Sleep(300);
                    }
                    log.Info("Time out detected. Restarting Stream.");
                    goto StreamStart;
                }
                else 
                {
                    log.Error("Error in stream",ex);
                    throw;
                }
            }
        }

        private static void processTweetList(List<Tweetinvi.Events.TweetReceivedEventArgs> tweetList) 
        {
            if(tweetList.Count > 0)
            {
                using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(Settings.Default.taskCount))
                {
                    var tasks = new List<Task>();
                    foreach (var arguments in tweetList)
                    {
                        concurrencySemaphore.Wait();

                        var t = Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                processTweet(arguments);
                            }
                            catch (Exception ex)
                            {
                                log.Error("Error processing tweet", ex);
                            }
                            finally
                            {
                                concurrencySemaphore.Release();
                            }
                        });

                        tasks.Add(t);
                    }
                }
            }
        }

        private static void processTweet(Tweetinvi.Events.TweetReceivedEventArgs arguments) 
        {
            if (arguments.Tweet.Hashtags.Count > 0)
            {
                arguments.Tweet.Hashtags.ForEach(x => processHashTags(x.ToString().ToLower()));
            }
        }

        private static void runReports() 
        {
            Console.Clear();
            Console.WriteLine("Howdy audience. This page will refresh itself with new statistics every 60 seconds.");
            Console.WriteLine("Thank you for your consideration.");
            Console.WriteLine();
            Console.WriteLine("Here are the latest statistics as of {0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString());
            Console.WriteLine("Total number of tweets received: {0}", tweetCounter.ToString());
            Console.WriteLine("Top 10 Hashtags: ");

            var sortedHashes = hashTags.OrderByDescending(entry => entry.Value).Take(10).ToDictionary(pair => pair.Key, pair => pair.Value);
            var x = 1;
            foreach(var hash in sortedHashes) 
            {
                Console.WriteLine("    Hashtag #{0}: {1} with a count of {2}", x.ToString(), hash.Key, hash.Value);
                x++;
            }

            if (Settings.Default.outputToFile) 
            {
                try 
                {
                    File.WriteAllText(@Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\hashtags.json", JsonConvert.SerializeObject(hashTags));
                }
                catch(Exception ex) 
                {
                    log.Error("Error outputting file to JSON", ex);
                }
            }
        }

       private static void processHashTags(string hashTag) 
       {
            if (!string.IsNullOrEmpty(hashTag)) 
            {
                if (!hashTags.ContainsKey(hashTag))
                {
                    hashTags[hashTag] = 1;
                }
                else
                {
                    var counter = hashTags[hashTag];
                    counter++;
                    hashTags[hashTag] = counter;
                }
            }
       }
    }
}