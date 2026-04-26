public enum ProjectType
{
    Unit,
    Building
}

public class CityProject
{
    public string name;
    public ProjectType type;
    public int cost;

    public CityProject(string name, ProjectType type, int cost)
    {
        this.name = name;
        this.type = type;
        this.cost = cost;
    }
}