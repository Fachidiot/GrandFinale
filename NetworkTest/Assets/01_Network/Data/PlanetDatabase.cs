using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "PlanetDatabase", menuName = "NetworkTest/Planet Database", order = 2)]
public class PlanetDatabase : ScriptableObject
{
    public List<PlanetData> allPlanets;

    public PlanetData GetPlanetById(int id)
    {
        return allPlanets.FirstOrDefault(p => p.planetId == id);
    }
}
