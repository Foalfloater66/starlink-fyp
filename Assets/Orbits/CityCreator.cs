using Scene.GameObjectScripts;
using UnityEngine;

namespace Orbits
{
    public class CityCreator
    {
        private readonly Transform global_transform;
        private readonly GameObject city_prefab;
        private readonly GroundstationCollection groundstations;

        /// <summary>
        /// Constructor for the CityCreator object. Creates cities and assigns them to groundstations.
        /// </summary>
        /// <param name="global_transform">Earth's transform object.</param>
        /// <param name="city_prefab">Prefab assigned to cities.</param>
        /// <param name="groundstations">Collection of groundstations.</param>
        public CityCreator(Transform global_transform, GameObject city_prefab, GroundstationCollection groundstations)
        {
            this.global_transform = global_transform;
            this.city_prefab = city_prefab;
            this.groundstations = groundstations;
        }
        
        /// <summary>
        /// Creates the top 25 most populated cities of USA as of the 2020 census.
        ///
        /// Source: https://en.wikipedia.org/wiki/List_of_United_States_cities_by_population
        /// </summary>
        public void USACities()
        {
            CreateCity(40.76f, 73.98f, "New York");
            CreateCity(34f, 118f,  "Los Angeles"); // Los Angeles
            CreateCity(25.768125f, 80.197006f,  "Miami");
            CreateCity(41.87f, 87.62f,  "Chicago"); //;
            CreateCity(29.76f, 95.36f,  "Houston"); //;
            CreateCity(33.44f, 112.07f,  "Phoenix"); //;
            CreateCity(39.95f, 75.16f,  "Philadelphia");
            CreateCity(29.42f, 98.49f,  "San Antonio"); // Texas
            CreateCity(32.71f, 117.16f,  "San Diego");
            CreateCity(32.46f, 96.47f,  "Dallas");
            CreateCity(30.26f, 97.74f,  "Austin");
            CreateCity(30.33f, 81.65f,  "Jacksonville");
            CreateCity(37.33f, 121.88f,  "San Jose");
            CreateCity(32.75f, 97.33f,  "Fort Worth");
            CreateCity(39.96f, 82.99f,  "Columbus");
            CreateCity(35.22f, 80.84f,  "Charlotte");
            CreateCity(39.76f, 86.15f, "Indianapolis");
            CreateCity(37.77f, 122.41f,  "San Francisco");
            CreateCity(47.60f, 122.33f,  "Seattle");
            CreateCity(39.73f, 104.99f,  "Denver");
            CreateCity(35.46f, 97.51f,  "Oklahoma City");
            CreateCity(36.16f, 86.78f,  "Nashville");
            CreateCity(31.76f, 106.48f,  "El Paso");
            CreateCity(38.90f, 77.03f,  "Washington DC");
            CreateCity(36.17f, 115.13f,  "Las Vegas");
            CreateCity(42.36f, 71.05f,  "Boston");
        }
        
        /// <summary>
        /// Creates the top 10 most populated cities of Canada as of the 2021 census.
        /// Source: https://en.wikipedia.org/wiki/List_of_largest_Canadian_cities_by_census
        /// </summary>
        public void CANCities()
        {
            CreateCity(43.65f, 79.38f, "Toronto");
            CreateCity(45.50f, 73.56f, "Montreal");
            CreateCity(51.04f, 114.07f, "Calgary");
            CreateCity(45.42f, 75.69f, "Ottawa");
            CreateCity(53.54f, 113.49f, "Edmonton");
            CreateCity(49.89f, 97.13f,  "Winnipeg");
            CreateCity(43.58f, 79.64f,  "Mississauga");
            CreateCity(49.28f, 123.12f,  "Vancouver");
            CreateCity(43.73f, 79.76f,  "Brampton");
            CreateCity(43.25f, 79.87f,  "Hamilton");
        }

        /// <summary>
        /// Creates 35 cities in North America.
        /// </summary>
        public void NACities()
        {
            USACities();
            CANCities();
        }
        
        /// <summary>
        /// Creates a city GameObject and stores it as a groundstation.
        /// </summary>
        /// <param name="latitude">City latitude coordinates.</param>
        /// <param name="longitude">City longitude coordinates.</param>
        /// <param name="city_name">City name.</param>
        public void CreateCity(float latitude, float longitude, string city_name)
        {
            GameObject city = 
                (GameObject) Object.Instantiate(city_prefab, new Vector3(0f, 0f, /*-6382.2f*/-6371.0f), global_transform.rotation);
		
            float long_offset = 20f;
            city.transform.RotateAround(Vector3.zero, Vector3.up, longitude - long_offset);
            Vector3 lat_axis = Quaternion.Euler(0f, -90f, 0f) * city.transform.position;
            city.transform.RotateAround(Vector3.zero, lat_axis, latitude);
            city.transform.SetParent(global_transform, false);
		
            // Assign a script to the city.
            CityScript cs = (CityScript)city.GetComponent(typeof(CityScript));
            cs.longitude = longitude;
            cs.latitude = latitude;
            cs.city_name = city_name; // TODO: make the target a different color, or not even a CityScript. (It's not correct!)

            // Add the city as a groundstation.
            groundstations.addGroundstation(city, city_name);
        }
    }
}