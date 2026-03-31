-- Weekly Leaderboard
CREATE OR REPLACE VIEW WeeklyLeaderboard AS
SELECT 
    u."UserID",
    u."UserName",
    COUNT(CASE WHEN m."Status" = 1 AND m."Player1ID" = u."UserID" THEN 1
               WHEN m."Status" = 2 AND m."Player2ID" = u."UserID" THEN 1 END) AS Wins,
    COUNT(m."MatchID") AS MatchesPlayed,
    ps."Rating",
    ps."SeasonNo"
FROM "User" u
LEFT JOIN "Match" m
    ON u."UserID" IN (m."Player1ID", m."Player2ID")
LEFT JOIN "PlayerStat" ps
    ON u."UserID" = ps."UserID"
WHERE m."FinishedAt" >= DATE_TRUNC('week', NOW()) -- Start of the current week (Monday 00:00)
  AND m."FinishedAt" < DATE_TRUNC('week', NOW() + INTERVAL '1 week') - INTERVAL '10 minutes' -- Exclude last 10 minutes of Sunday
GROUP BY u."UserID", u."UserName", ps."Rating", ps."SeasonNo"
ORDER BY ps."SeasonNo" DESC, Wins DESC, MatchesPlayed DESC, ps."Rating" DESC;

-- Monthly Leaderboard
CREATE OR REPLACE VIEW MonthlyLeaderboard AS
SELECT 
    u."UserID",
    u."UserName",
    COUNT(CASE WHEN m."Status" = 1 AND m."Player1ID" = u."UserID" THEN 1
               WHEN m."Status" = 2 AND m."Player2ID" = u."UserID" THEN 1 END) AS Wins,
    COUNT(m."MatchID") AS MatchesPlayed,
    ps."Rating",
    ps."SeasonNo"
FROM "User" u
LEFT JOIN "Match" m
    ON u."UserID" IN (m."Player1ID", m."Player2ID")
LEFT JOIN "PlayerStat" ps
    ON u."UserID" = ps."UserID"
WHERE m."FinishedAt" >= DATE_TRUNC('month', NOW()) -- Start of the current month
  AND m."FinishedAt" < DATE_TRUNC('month', NOW() + INTERVAL '1 month') - INTERVAL '10 minutes' -- End of the current month
GROUP BY u."UserID", u."UserName", ps."Rating", ps."SeasonNo"
ORDER BY ps."SeasonNo" DESC, Wins DESC, MatchesPlayed DESC, ps."Rating" DESC;

-- All-Time Leaderboard
CREATE OR REPLACE VIEW AllTimeLeaderboard AS
SELECT 
    u."UserID",
    u."UserName",
    COUNT(CASE WHEN m."Status" = 1 AND m."Player1ID" = u."UserID" THEN 1
               WHEN m."Status" = 2 AND m."Player2ID" = u."UserID" THEN 1 END) AS Wins,
    COUNT(m."MatchID") AS MatchesPlayed,
    ps."Rating",
    ps."SeasonNo"
FROM "User" u
LEFT JOIN "Match" m
    ON u."UserID" IN (m."Player1ID", m."Player2ID")
LEFT JOIN "PlayerStat" ps
    ON u."UserID" = ps."UserID"
GROUP BY u."UserID", u."UserName", ps."Rating", ps."SeasonNo"
ORDER BY ps."SeasonNo" DESC, Wins DESC, MatchesPlayed DESC, ps."Rating" DESC;