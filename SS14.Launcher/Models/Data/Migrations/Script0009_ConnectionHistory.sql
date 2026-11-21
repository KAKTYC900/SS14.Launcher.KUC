-- History of game sessions started from the launcher.
CREATE TABLE ConnectionHistory (
    -- Client-generated GUID (as text), so a session can be finished later without knowing a DB row id.
    Id TEXT NOT NULL PRIMARY KEY,

    -- Address the launcher connected to, as given when connecting.
    Address TEXT NOT NULL,

    -- Name of the favorite at the time, if any.
    Name TEXT NULL,

    -- Unix time in seconds.
    StartTime INTEGER NOT NULL,

    -- Unix time in seconds. NULL while the session runs (or if the launcher was closed before it ended).
    EndTime INTEGER NULL
);

CREATE INDEX ConnectionHistoryStartTime ON ConnectionHistory (StartTime);
