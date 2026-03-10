--------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------- *TABLES* -----------------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------

CREATE TABLE "User" (
    "UserID" SERIAL PRIMARY KEY,
    "UserName" VARCHAR(100) UNIQUE,
        CONSTRAINT "Proper_UserName" CHECK ("UserName" ~* '^[A-Za-z0-9_]+$'),
    "Email" VARCHAR(100) NOT NULL UNIQUE 
        CONSTRAINT "Proper_Email" CHECK ("Email" ~* '^[A-Za-z0-9._+%-]+@[A-Za-z0-9.-]+[.][A-Za-z]+$'),
    "PasswordHash" VARCHAR(255) NOT NULL,
    -- 0 = Player, 1 = AdminPlayer, 2 = Admin
    "Role" INT NOT NULL DEFAULT 0
        CHECK ("Role" BETWEEN 0 AND 2),
    "Rating" INT NOT NULL DEFAULT 1000,
    "MatchesPlayed" INT DEFAULT 0,
    "IsBanned" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "LastUpdatedAt" TIMESTAMPTZ
);

CREATE TABLE "Session" (
    "SessionID" SERIAL PRIMARY KEY,
    "UserID" INT NOT NULL REFERENCES "User"("UserID"),
    "RefreshToken" VARCHAR(255) NOT NULL UNIQUE,
    "RT_ExpiresAt" TIMESTAMPTZ NOT NULL,
    "AccessToken" VARCHAR(255) NOT NULL UNIQUE,
    "AT_ExpiresAt" TIMESTAMPTZ NOT NULL,
    "LastLogin" TIMESTAMPTZ NOT NULL,
    "IsRevoked" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "LastUpdatedAt" TIMESTAMPTZ
);

CREATE TABLE "Match" (
    "MatchID" SERIAL PRIMARY KEY,
    "Player1ID" INT NOT NULL REFERENCES "User"("UserID"),
    "Player2ID" INT NOT NULL REFERENCES "User"("UserID"),
    "P1RatingChange" INT,
    "P2RatingChange" INT,
    -- 0 = Abondoned, 1 = P1 Won, 2 = P2 Won, 3 = In Progress, 4 = Placing Ships, 5 = Waiting/Loading
    "Status" INT NOT NULL DEFAULT '5'
        CHECK ("Status" BETWEEN 0 AND 5),
    "TotalNoOfTurns" INT NOT NULL DEFAULT 0,
    "FinishReason" VARCHAR(255),
    "StartedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "FinishedAt" TIMESTAMPTZ
);

CREATE TABLE "MatchMove" (
    "MoveID" SERIAL PRIMARY KEY,
    "PlayerID" INT NOT NULL REFERENCES "User"("UserID"),
    "MatchID" INT NOT NULL REFERENCES "Match"("MatchID"),
    "MoveNumber" INT NOT NULL,
    "HitX" INT NOT NULL,
    "HitY" INT NOT NULL,
    "Result" BOOLEAN NOT NULL,
    "TimeOfMove" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "ShipType" (
    "ShipID" SERIAL PRIMARY KEY,
    "ShipName" VARCHAR(50) NOT NULL UNIQUE,
    "Length" INT NOT NULL CHECK ("Length" > 0),
    "Width" INT NOT NULL CHECK ("Width" > 0),
    "MaxPerPlayer" INT NOT NULL CHECK ("MaxPerPlayer" > 0)
);

CREATE TABLE "ShipPlacement" (
    "PlacementID" SERIAL PRIMARY KEY,
    "PlayerID" INT NOT NULL REFERENCES "User"("UserID"),
    "MatchID" INT NOT NULL REFERENCES "Match"("MatchID"),
    "ShipID" INT NOT NULL REFERENCES "ShipType"("ShipID"),
    "StartX" INT NOT NULL,
    "StartY" INT NOT NULL,
    "IsVertical" BOOLEAN NOT NULL,
    "PlacedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "Spectator" (
    "SpectatorID" INT NOT NULL REFERENCES "User"("UserID"),
    "MatchID" INT NOT NULL REFERENCES "Match"("MatchID"),
        PRIMARY KEY ("SpectatorID", "MatchID"),
        UNIQUE ("SpectatorID", "MatchID"),
    "JoinedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Duration" INTERVAL DEFAULT '0 seconds'
);

CREATE TABLE "MatchmakingQueue" (
    "QueueID" SERIAL PRIMARY KEY,
    "PlayerID" INT NOT NULL REFERENCES "User"("UserID"),
    UNIQUE ("QueueID", "PlayerID"), -- Ensure a player can only be in the queue once
    -- We will delete the player from this table once they are matched. 
    -- So, we don't need an "IsActive" column. And the table won't become too big.
    "JoinedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "BanList" (
    "BanID" SERIAL PRIMARY KEY,
    "AdminID" INT NOT NULL REFERENCES "User"("UserID"),
    "PlayerID" INT NOT NULL REFERENCES "User"("UserID"),
    "IsReverted" BOOLEAN NOT NULL DEFAULT FALSE,
    "Reason" VARCHAR(255) NOT NULL,
    "IsTemporary" BOOLEAN NOT NULL DEFAULT FALSE,
    "BannedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Duration" INTERVAL, -- Only applicable for Temporary bans
   
    "BannedUntil" TIMESTAMPTZ -- Calculated based on Duration for Temporary bans
);

--------------------------------------------------------------------------------------------------------------------------------
--------------------------------------------------------- *INSERTIONS* ---------------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------

INSERT INTO "ShipType" ("ShipName", "Length", "Width", "MaxPerPlayer") VALUES
('Carrier',    5, 1, 1),
('Battleship', 4, 1, 1),
('Cruiser',    3, 1, 2),
('Submarine',  3, 1, 2),
('Destroyer',  2, 1, 2);