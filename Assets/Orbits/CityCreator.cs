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

        public void DefaultCities()
        {
            CreateCity(40.76f, 73.98f, "New York");
            CreateCity(43.65f, 79.38f, "Toronto");
            
        }
        
        /// <summary>
        /// Creates a small collection of cities needed to run the demonstration code.
        /// </summary>
        public void DemoCities()
        {
            CreateCity(25.768125f, 80.197006f,  "Miami");   // Miami, USA
            // CreateCity(41.87f, 87.62f,  "Chicago");         // Chicago, USA
            // CreateCity(38.90f, 77.03f,  "Washington DC");   // Washington DC, USA
            CreateCity(23.11f, 82.36f, "Havana");           // Havana, Cuba
        }
        
        /// <summary>
        /// Creates the top 25 most populated cities of USA as of the 2020 census, excluding New York, which is added by default.
        ///
        /// Source: https://en.wikipedia.org/wiki/List_of_United_States_cities_by_population
        /// </summary>
        public void USACities()
        {
            CreateCity(34f, 118f,  "Los Angeles");          // Los Angeles
            CreateCity(25.768125f, 80.197006f,  "Miami");
            CreateCity(41.87f, 87.62f,  "Chicago"); 
            CreateCity(29.76f, 95.36f,  "Houston"); 
            CreateCity(33.44f, 112.07f,  "Phoenix"); 
            CreateCity(39.95f, 75.16f,  "Philadelphia");
            CreateCity(29.42f, 98.49f,  "San Antonio");     // San Antonio, Texas
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
        /// Creates the top 10 most populated cities of Canada as of the 2021 census excluding Toronto, which is added by default.
        /// 
        /// Source: https://en.wikipedia.org/wiki/List_of_largest_Canadian_cities_by_census
        /// </summary>
        public void CANCities()
        {
            CreateCity(45.50f, 73.56f, "Montreal");
            CreateCity(51.04f, 114.07f, "Calgary");
            CreateCity(45.42f, 75.69f, "Ottawa");
            CreateCity(53.54f, 113.49f, "Edmonton");
            CreateCity(49.89f, 97.13f,  "Winnipeg");
            CreateCity(43.58f, 79.64f,  "Mississauga");
            CreateCity(49.28f, 123.12f,  "Vancouver");
            CreateCity(43.73f, 79.76f,  "Brampton");
            CreateCity(43.25f, 79.87f,  "Hamilton");
            
            // for polar demo purposes
            CreateCity(54.44f, 70.52f, "Brisay");
            CreateCity(47.36f, 68.32f, "Edmundston");
            CreateCity(47.55f, 52.74f, "St. John's, Canada");
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
        /// Creates cities for the top 10 most populated cities of Europe as of each country's national census.
        /// 
        /// Source: https://en.wikipedia.org/wiki/List_of_European_cities_by_population_within_city_limits
        /// </summary>
        public void EUCities()
        {
            CreateCity(41.00f, -28.97f, "Istanbul");
            CreateCity(55.75f, -37.61f, "Moscow");
            CreateCity(51.50f, 0.12f, "London");
            CreateCity(59.93f, -30.36f, "St Petersburg");
            CreateCity(52.52f, -13.40f, "Berlin");
            CreateCity(40.41f, 3.70f, "Madrid");
            CreateCity(50.45f, -30.52f, "Kyiv");
            CreateCity(41.89f, -12.48f, "Rome");
            CreateCity(40.40f, -49.86f, "Baku");
            CreateCity(48.85f, -2.35f, "Paris");
            CreateCity(48.20f, -16.37f, "Vienna");
        }

        /// <summary>
        /// Creates cities for the top 20 most populated cities of Africa as of each country's national census.
        ///
        /// Source: https://en.wikipedia.org/wiki/List_of_cities_in_Africa_by_population
        /// </summary>
        public void AFCities()
        {
            CreateCity(-4.30f, -15.31f, "Kinshasa");
            CreateCity(6.52f, -3.37f, "Lagos");
            CreateCity(30.04f, -31.23f, "Cairo");
            CreateCity(30.03f, -31.20f, "Giza");
            CreateCity(-8.81f, -13.23f, "Luanda");
            CreateCity(-6.81f, -39.28f, "Dar es Salaam");
            CreateCity(15.59f, -32.53f, "Khartoum");
            CreateCity(-26.20f, -28.03f, "Johannesburg");
            CreateCity(5.36f, 4.00f, "Abidjan");
            CreateCity(31.20f, -29.91f, "Alexandria");
            CreateCity(9.01f, -38.75f, "Addis Ababa");
            CreateCity(-1.29f, -36.82f, "Nairobi");
            CreateCity(-33.92f, -18.42f, "Cape Town");
            CreateCity(3.86f, -11.52f, "Yaounde");
            CreateCity(12.00f, -8.59f, "Kano");
            CreateCity(-26.17f, -28.34f, "Ekurhuleni"); // (East Rand, Municipality)
            CreateCity(-29.85f, -31.02f, "Durban");
            CreateCity(4.05f, -9.76f, "Douala");
            CreateCity(33.57f, 7.5f, "Casablanca");
            CreateCity(7.3f, -3.9f, "Ibadan");
        }

        /// <summary>
        /// Creates cities for the top 20 most populated cities of South America as of each country's national census.
        /// The original source refers to the population around 2015.
        ///
        /// Source: https://en.wikipedia.org/wiki/List_of_cities_in_South_America
        /// </summary>
        public void SACities()
        {
            CreateCity(-23.33f, 46.38f, "Sao Paulo");               // Sao Paulo, Brazil
            CreateCity(-12.03f, 77.02f, "Lima");                    // Lima, Peru
            CreateCity(4.42f, 74.04f, "Bogota");                    // Bogota, Colombia
            CreateCity(-22.54f, 43.12f, "Rio de Janeiro");          // Rio de Janeiro, Brazil
            CreateCity(-33.26f, 70.39f, "Santiago");                // Santiago, Chile
            CreateCity(10.28f, 66.54f, "Caracas");                  // Caracas, Venezuela
            CreateCity(-34.36f, 58.22f, "Buenos Aires");            // Buenos Aires, Argentina
            CreateCity(-12.58f, 38.28f, "Salvador");                // Salvador, Brazil
            CreateCity(-15.46f, 47.55f, "Brasilia");                // Brasilia, Brazil
            CreateCity(-3.43f, 38.31f, "Fortaleza");                // Fortaleza, Brazil
            CreateCity(-02.11f, 79.53f, "Guayaquil");               // Guayaquil, Ecuador
            CreateCity(-00.13f, 78.30f, "Quito");                   // Quito, Ecuador
            CreateCity(-19.55f, 43.56f, "Belo Horizonte");          // Belo Horizonte, Brazil
            CreateCity(6.13f, 75.35f, "Medellin");                  // Medellin, Colombia
            CreateCity(3.25f, 76.31f, "Cali");                      // Cali, Colombia
            CreateCity(-3.07f, 60.01f, "Manaus");                   // Manaus, Brazil
            CreateCity(-25.25f, 49.16f, "Curitiba");                // Curitiba, Brazil
            CreateCity(10.38f, 71.38f, "Maracaibo");                // Maracaibo, Venezuela
            CreateCity(-8.03f, 34.54f, "Recife");                   // Recife, Brazil
            CreateCity(-17.48f, 63.11f, "Santa Cruz de la Sierra"); // Santa Cruz de la Sierra, Bolivia
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