# The Twitter Test

For Jack Henry & Associates.  Thank you for your consideration.  

## Getting Started


### Dependencies

* Utilizes the Tweetinvi NuGet Package 5.0.4
* Minimum Windows Framework should be a minimum of 4.6.1

### Installing

The App.config file contain values that will need to be adjusted. 
* twitterConsumerKey (System.String)- Twitter Consumer API Key
* twitterConsumerSecret (System.String)- Twitter Consumer API Secret
* twitterAccessToken (System.String)- Twitter Authentication Access Token
* twitterAccessTokenSecret (System.String)- Twitter Authentication Access Secret
* onlyEnglish (System.Boolean) - If True then an English only language filter will be applied.
* taskCount (System.Int32) - Determines the maximum number of concurrent tasks that can process tweets at once. This number can be moved higher for stronger machines and should never be at or larger than the number of cores on the CPU of the processing machine.  
* outputToFile (System.Boolean) - It True then the hashtags will be ouputed to a JSON file in the root directory.

## Version History

* 0.1
    * Initial Release
