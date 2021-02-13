# MockTwitterAPI

This is my implementation of Twitter's backend API for speer.io's technical assessment. I choose to use ASP.NET Core for this project.

# Section 1 unit test instructions
The unit tests are not as automated as I would have liked them to be, but they still work and are fairly comprehensive. 

To start, import the section 1 postman.json file from the PostmanTests folder into Postman. 

Let's start by showing that the API functions properly.
1. Open the collection you just imported and open the folder called Registration/Login/JWT verification success.
2. In here are 3 requests. Click the first request, open the request body and select the raw JSON format. Since the database should be empty, there should be no username collisions so you can just leave the default username and execute the request. If the database is not empty (you've been registering users) you can replace your .db file with the .db file in this git repo. You may be prompted to disable certificate validation, please disable it. The response will indicate that the account was created. 
3. We can now move to the next request in the folder. Here we will be using the account we just created to get a JWT from the server. Click the request and make sure the body is identical to the body of the first request we executed (same username and password). Execute the request and the response body will be the JSON Web Token for this account. Copy it to the clipboard and proceed to click on the next request. 
4. In this final request, click the authorization tab. Select the "Bearer Token" type and then paste the token you copied earlier into the text field to the right. Upon executing the request, the response will indicate that you are logged in as the username you had registered as, showing that the registration/login system is functioning.
