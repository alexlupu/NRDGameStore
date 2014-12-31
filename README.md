# XBox Store Page

## Basic Requirements

Store page that manages the purchasing suggestions for a company's supply of XBox games.

### Actions Allowed Once Per Workday Per Employee
 1. Vote for a game title
 2. Suggest a new game title
 

### Actions Allowed Without Restriction
 1. Mark a "voted" game as purchased 



# Design Notes

## Architecture

( SOAP Service -> Service Manager -> Business Manager ) -> Web Forms Front-end

The service proxy, its manager, and the business rules are contained in their own project, which is in turn referenced by a Web Forms project.
In this way, should one want to add a MVC front-end, it could be done very easily by adding a new project and adding the reference.

## Soap Service

Already-provided, with functions:
 1. CheckKey (check API key)
 2. GetGames
 3. AddVote
 4. AddGame
 5. SetGotIt (mark game as owned)
 6. ClearGames (reset library)

To interact with the Service Manager, I added a service reference to the SOAP service, and let Visual Studio create the proxy objects.

### Quirks with the Service
From interacting with the SOAP service, and running some integration tests, I've come to find that the SOAP-service has a few quirks that can be prevented in the Service Manager, but should, ideally, be solved on the SOAP Service side.
The first being if one adds a game, it also counts as a single vote. This is in no way a bad thing, but it means that one can only suggest new games during the "voting" whereas, I don't believe that they should be linked.
An example of this would be if we started a new office, and the employees brought with them their old XBox games, and we wanted to add these games to the inventory without going through the submission / voting process.
A second quirk is that there's no restriction in place on duplicate game titles. I'll come back to this later, but it seems that it would be trivial to add an index, if it were a DB store, though we don't have insight into that part. 
Another quirk of the service is that if you add mark a game as owned that is already owned, then it returns true (fair) but also increments the vote count, which makes little sense.
Finally, the most "dangerous" quirk in terms of data integrity, as far as I've seen, is that if one adds a vote for a game that is already marked as owned, it increments the vote count, but also switches the status from owned to wanted.

The result of these quirks is that the Service and Business Managers, as well as the Front-end, need to work extra to prevent these from taking place, and can lead to a solution that's not as intuitive.
I'll be touching on some of these, and my approaches to tackling them, in the later descriptions.


## Service Manager


### Interface and Modifiers
I designed this class as a simple wrapper around the SOAP web service calls, with slightly clearer names for the methods.
Should we have changed it to a different service, or a direct connection to the DB, I would create an interface for the Service Manager with the public methods listed.
Then, we could have a SOAPServiceManager and DBServiceManager inherit and change their implementation appropriately.
The class is marked as public, but its use is encouraged through the Business Manager. 
We could change the modifier to make internal if we wanted to restrict its use outside the encouraged case.

### Constructor and Connection Test
I've left the default empty constructor, though I had considered adding the check for IsConnectionWorking inside, and throwing an exception if it did not work.
On the one hand, what could be an exception at one point in time which might go away (e.g. newly-set API key registers after a constructor is set), it shouldn't force developers to create a try/catch around constructors.
On the other hand, those referencing this manager should be required to read the documentation, and make sure to check IsConnectionWorking shortly after the Manager is instantiated, or just before calling any of the public methods.
I have chosen the latter, since I dislike try/catch blocks around constructors more than around methods that would imply that a connection is going to be attempted.

### IsConnectionWorking
This method could be used to verify our service connection is up and running, in this case, it checks if the stored API key is valid.
If it were a DB connection, it could test the SQL connection string, for example.
I have chosen to allow the business manager to call this method as many times as it deems necessary rather than force it to be called before every method from the Service Manager itself.
Should we want to limit the number of times it does get called, we can make it private and let this manager deal with it exclusively.

### API Key Store
I have chosen to store the API Key in a Settings file, mostly due to the ease-of-use should we need to change it and other keys. 
From my experience, API keys change very slowly, and so, I elected for it to be kept in the settings file. 
If it was the case that we needed to change API keys frequently, I would move that to a run-time assignment, and allow for some sort of user-entry or auto-generation of new keys, as needed.

### Async
Having no insight into the storage method, I am making an assumption that the "slowest" method call to the service would be the fetching of the entire list of games (GetGames), 
especially since the interface doesn't provide a way to ask for only X amount of games, or a paginated variant (1-20, 21-40, etc.). 
It's also the method that stands to have the most data transferred, since it's the only endpoint that doesn't respond with a single boolean. 
As such, I have provided an async implementation for it as well as a regular synchronous one. 
If it was made clear that adding a new title or vote was also slow, I would have also provided async implementations for these, using GetAllGamesAsync as a basic template of the implementation.

### Error Handling
In the interest of simplicity, I have kept the error handling to a minimum at this stage, choosing to defer these decisions down the pipeline.
I have, however, abstracted handling SOAP-specific FaultException into one method, HandleServiceExceptions, where we could add specific logic or logging, as needed.
This is so that the Business Manager would not have to know implementation-specific error handling. 

### Service Client and Disposal
I have chosen to dispose of the service client after each call to its service. This is so that it may release its resources as soon as possible, which would be favourable should the usage of the service scale greatly. 
It could be that creating and disposing the service client introduces a significant overhead, at which point we could look into setting its lifetime to be the same of the Service Manager and disposing of it at that time.
My research into services also identified a gotcha with Service client disposal within a using-statement:
http://stackoverflow.com/questions/573872/
Therefore, I have taken the suggestion of the above SO answers and this blog post:
http://coding.abel.nu/2012/02/using-and-disposing-of-wcf-clients/
and added the partial class for the SOAP client appropriately.

### Fetching Games and Caching
The requirement states that the interface should communicate in real-time and display the most current information.
If there was some wiggle-room there, I would add caching around the GetGames calls, to make sure that we're not inundating the service with repeated calls for the list of games.
In the interest of time, I have left the application without a caching solution, though, if I were to have more time to spend on it, I most likely would have added it.
My reasoning for wanting caching is that there are business decisions (no two games with the same title) that the SOAP service (or DB back-end) does not enforce.
This means that for each action that writes to the DB, I need to re-fetch the list of games just before executing the action so that I may run the business rules against the data before writing it to store.
Caching would allow for these extra calls to be virtually free, making this extra step relatively painless, but as such, the more users, the greater the risk to data integrity.


## Business Manager


### IsConnectionWorking
Similarly to Service Manager, I considered running this check in the constructor, before each function, or not at all, and letting the implementer of Business Manager decide. 
I opted for the last option, since I have control of the front-end, though if this were a manager open to many, I might force the connection test in the constructor, since it would avoid calling the endpoint repeatedly, or not at all.
I believe that it should be made difficult to integrate with a service in a wrong way (in this case, spam the SOAP endpoint connection with tests). 
Having said this, I found the code a lot simpler to follow if we just entrusted tests of the connection string to the next step in the pipeline. 

### Time-based Business Rules
I chose to store the days of the week which allowed voting as well as the office time zone in the local settings file. 
This way, extending these to different days of the week, or incorporating an office in a different time zone would be straightforward.
All of the dates used in these checks are in UTC, taking out much of the headaches.

### Async Fetching
I provided an async option for fetching the entire list of games, and let the UI decide. As mentioned before, if we needed to use Async calls for other methods, we'd copy these examples.

### Repeated Fetches
Due to the situation with caching and service quirks (mentioned above), I decided the best way to avoid integrity issues in the data is to fetch the entire list of games before each time we're adding or voting for a game, or marking it as owned.
This is quite inefficient, but cuts the risk greatly. Ideally, we'd have a method to fetch only a specific game by ID or title, which should be a less expensive call.

### User Management
I have deferred user management to the front-end, letting them worry about cookies or authentication. Time-permitting, I would have added a bunch more robustness either in this Business Manager, or a separate User Manager. 
As such, the only factor that I look at related to users is the time, in UTC, of their last vote or title submission, which gives me enough to meet the requirements.


## Front-End


### Web Forms
Interfaces are not my strongest point, and so, I chose a quick-and-dirty approach to demonstrate the functionality of the web application.
I chose cookies to validate users, though given more time, would have created a more robust voting relational store.
Where editing permissions are not present (vote already cast), I have disabled the inputs, and where data is missing, I have replaced an empty list with a friendly message.

### JavaScript
I have also avoided using too much JavaScript, mostly due to time-restraints, since my proficiency in it is not as high as the server code, and therefore would have taken longer to come to a solution.
Since I had relied upon using JavaScript in some variant, I opted to add jQuery, since it would be very easy to "pretty" up a lot of the interface with some of its libraries and plugins.

### Store Page
This is the main page, used to see the list of owned games, games to vote for (and their vote count), and finally, a way of adding a new game. 
This is the page that reads and writes cookes to and from the browser and uses the results for our restrictions, and does so using HttpOnly flags to help prevent some cases of XSS. 
All times that the cookies store are set in UTC to make it much easier to deal with on the server-side.
When taking new user titles, this page HTML-encodes the values from the title input box, again, to avoid XSS issues.

### Owned Page
This page is the "Admin Form", unlinked from the Store Page, which allows someone to mark a game as owned with no restrictions in place aside from picking a game presented in the list.
With more time, and better user management, we could restrict this page to certain groups or individual, but for now security through obscurity of not giving the name out, should suffice.


## Testing


I ran out of time when trying to mock the SOAP service, so most of my tests are, in fact, integration tests, since they needed to connect to the live SOAP service.
They generally follow the pattern of initialising the library to a particular starting point, and testing corner cases and the quirks mentioned earlier.

