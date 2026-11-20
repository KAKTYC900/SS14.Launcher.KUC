-- Binds a launcher account to a server address.
-- When connecting to a bound server, the launcher switches to this account automatically.
CREATE TABLE ServerAccountBinding (
    -- Normalized "host:port" of the server (see DataManager.NormalizeServerAddress).
    Address TEXT NOT NULL PRIMARY KEY,

    -- UserId of the account in the Login table (GUID).
    -- Not a real foreign key: SQLite does not enforce those unless asked to,
    -- so the launcher removes bindings itself when a login is removed.
    UserId TEXT NOT NULL,

    CONSTRAINT AddressNotEmpty CHECK (Address <> '')
);
