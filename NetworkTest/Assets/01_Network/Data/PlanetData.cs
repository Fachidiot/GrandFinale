using UnityEngine;

[CreateAssetMenu(fileName = "PlanetData", menuName = "NetworkTest/Planet Data", order = 1)]
public class PlanetData : ScriptableObject
{
    public int planetId;
    public string planetName;
    public string sceneName;
    // Could add more data here later, like description, icon, etc.
}
