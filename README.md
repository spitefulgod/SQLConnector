# SQLConnector
Allows connection to SQL Database without the use of Entity.

Use as follows
// Connect to a database stored as a connection string, return the table as an object for passing back from a webservice as JSON
using(SQLConnector Connection = new SQLConnector("Name of Connection String")
{
  int Return = 0;
  // Fire the stored Procedure SetCurrentWorkspace with the given parameters UserID & WorkspaceID, the return value is placed intot Return
  object Result = Connection.GetObjectSetFromSp("SetCurrentWorkspace", ref Return, UserID, WorkspaceID);
  return Result;
}

// Connect to a database via connection details, return a DataTableCollection object.
using(SQLConnector Connection = new SQLConnector(ServerName, Database, Username, Password, 100)
{
  int Return = 0;
  DataTableCollection Result = Connection.GetDataSetFromSP("SetCurrentWorkspace", ref Return, UserID, WorkspaceID);
  return Result;
}

// Other function, execute a simple SQL
DataTableCollection Result = ExecuteSQLR("SELECT * FROM MyTable");


// Execute a stored procedure with no results
ExecuteSP("SetCurrentWorkspace", ref Return, UserID, WorkspaceID);

Stored procedure parameters a cached so that only a single DB run is fired after the initial hit.
