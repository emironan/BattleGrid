# BattleGrid

	To run the project, you will need .NET 10 SDK and PostgreSQL(v18+) 
		installed on your machine.
		
		IMPORTANT: If you are installing PostgreSQL for the first time, 
			make sure to set the locale setting by hand; 
			choose any language you want. Leaving it as default may cause problems!

	Then, I suggest you set the password for PostgreSQL to 1234 for simplicity. 
		If you choose a different password, make sure to update the connection string 
		in the appsettings.json and appsettings.Development.json files 
		in the BattleGrid.API folder.
	Make sure the port setting for PostgreSQL is 5432, which is the default.
	
	Open the repo with your IDE of choice (Visual Studio is preferred).
	
	First things first, you need to create the database. Open pgAdmin4.
	Create the DB with the exact name "BattleGridDB", under Databases.
	Find the SQL codes for the DB in the BattleGrid.Infrastructure/DatabaseCodes
		Start with 01_Tables_and_Indexed_vX.sql (latest version) to create the tables.
		Then, run 02_SP_and_Triggers_vX.sql, and then 03_Views_vX.sql queries.
		They are named 01, 02, 03 to indicate the order of execution for Docker.

	Back to the IDE. If you view via solution explorer,
		You will see there are currently 2 parts: src and tests.
	The src contains 3 different projects, 
		the server backend, a console app for testing the game logic, and a blazor server frontend.

	---------------------------------------------------------------------------------------------------
	
		- Server backend. This is the main part of the project.
		  It will handle all the game logic and communication with clients.
		  It is built using ASP.NET Core and RESTful APIs
			and SignalR will be implemented in the future for real-time 
			communication/handling of user interactions.
		  
		  API, Application, Contracts, Domain, and Infrastructure belong to server backend.

		  For now, API endpoints include register a user, get user info, 
			get ship type list and save ship placement in DB.
		  Game logic is implemented in the backend, in BattleGrid.Domain/GameLogic folder.
		  It will not be implemented with API controllers, but rather with SignalR hubs in the future.

		  Open a terminal window in your IDE and in the main project folder,
			run the commands in order:
			
			dotnet build		/* This will build the entire solution, all 3 projects:
								 * Server, Console and Tests;
						 		 *  and restore any necessary packages.
						 		 */
			cd BattleGrid.API	// This will move to the server backend project folder
			dotnet run			/* Or preferably, choose the BattleGrid.API project 
								 * as the startup project in VS and run it. This way
						 	 	 *  you can choose between https, http or IIS Express.
						 	 	 */

		  Running it directly via VS will open a new browser window with 
			the API documentation (Swagger UI) where you can test the API endpoints.

		  If you run it via a `dotnet run` command, only http works. 
			Open your browser and go to http://localhost:4744/swagger

	---------------------------------------------------------------------------------------------------

		- Console app. It is used to test the game logic. 2 ships per player is pre-placed.
		  Then, starting with Player 1, players will take turns to enter coordinates 
			to attack the opponent's ships.
		  And we control if the game flow is working correctly 
			and if the game end condition is detected properly.

		  Again, open another terminal or do a `cd ..` in the first one

			cd BattleGrid.Console	// Moves to the console app project folder
			dotnet run				/* This will launch a new terminal window
									 *	where you can play the game.
									 */
									 
	---------------------------------------------------------------------------------------------------

		- Blazor Server (BattleGrid.Web) is a blazor web app with interactive render mode set to server.
		  For now, it includes register, login, and some retrieve and display data functionality.

		  Open a new terminal

		  cd BattleGrid.Web
		  dotnet run

		  Then go to http://localhost:4745 and the home page will greet your. 
		  Try and test it!
		  
	---------------------------------------------------------------------------------------------------
		
		- Tests. Unit tests for the backend. It is built using xUnit.
		  Currently, it only includes tests for game logic. Such as,
		   - Coordinate validation
		   - Ship placement validation
		   - Player turn management
		   - Hit registration
		   - Game end detection
		  
		  It does not include tests for API endpoints, yet. 
		  We may implement it in the future.

		  Open another terminal or do a `cd ..` in the first one

			cd BattleGrid.Tests	// Moves to the test project folder
			dotnet test			/* This will run all the tests in the project
								 *	and show the results in the terminal.
								 */

		  Or, you can open the Test Explorer in VS (Test > Test Explorer) 
			where you can run and debug tests individually or all at once.
