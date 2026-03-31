# BattleGrid

	To run the project, you will need .NET 10 SDK installed on your machine.
	Open the repo with your IDE of choice (Visual Studio is preferred).

	Then you will see there are currently 3 parts/projects:

		- Server backend. This is the main part of the project.
		  It will handle all the game logic and communication with clients.
		  It is built using ASP.NET Core and RESTful APIs.
		   and SignalR will be implemented in the future for real-time communication/handling of user interactions.
		  
		  API, Applicaion, Contracts, Domain, and Infrastructure belong to this part

		  For now, API endpoints include register a user, get user info, get ship type list and save ship placement in DB.
		  Game logic is implemented in the backend, but it is not fully integrated with the API endpoints yet.

		  Open a terminal window in your IDE and in the main project folder run the commands in order:
			 dotnet build		/* This will build the entire solution (including all 3 projects)
						 *  and restore any necessary packages.
						 */
			 cd BattleGrid.API	// This will move to the server backend project folder
			 dotnet run		/* Or preferably, choose the BattleGrid.API project as the startup project in VS and run it.
						 * You can choose between https, http or IIS Express. It should all work fine.
						 */

		  Running it directly via VS will open a new browser window with the API documentation (Swagger UI)
		   where you can test the API endpoints.

		  If you run it via a `dotnet run` command, only http works. So, open your browser and go to http://localhost:4744 

		
		- Tests. Unit tests for the backend. It is built using xUnit.
		  Currently, it only includes tests for game logic functionalities such as,
		   - Coordinate validation
		   - Ship placement validation
		   - Player turn management
		   - Hit registration
		   - Game end detection
		  
		  It does not include tests for API endpoints, yet. We may implement it in the future.

		    Open another terminal or do a `cd ..` in the first one
			cd BattleGrid.Tests	// Moves to the test project folder
			dotnet test		// This will run all the tests in the project and show the results in the terminal.
						/* Or, you can open the Test Explorer in VS (Test > Test Explorer) 
						 * This will open the Test Explorer 
						 *  where you can run and debug tests individually or all at once. 
						 */


		- Console app. It is used to test the game logic. 2 ships per player is pre-placed.
		  Then, starting with Player 1, players will take turns to enter coordinates to attack the opponent's ships.
		  And we control if the game flow is working correctly and if the game end condition is detected properly.

		    Again, open another terminal or do a `cd ..` in the first one
			cd BattleGrid.Console	// Moves to the console app project folder
			dotnet run		// This will launch a new terminal window where you can play the game.