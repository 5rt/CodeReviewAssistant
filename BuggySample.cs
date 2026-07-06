public class BuggySample
{
    public string GetUser(string name)
    {
        var query = "SELECT * FROM Users WHERE name = '" + name + "'";
        return db.Run(query);
    }
}
