using System.Collections.Generic;
using Attack;
using Attack.Cases;
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
//         
//         /// <summary>
//         /// Create cities and place them on planet Earth.
//         /// </summary>
//         public void AddCities(CaseChoice caseChoice, Direction targetLinkDirection)
//         {
// // TODO: this is more of an attackCases thing.
//             switch (caseChoice)
//             {
//                 case CaseChoice.SimpleDemo:
//                     DemoCities();
//                     break;
//                 case CaseChoice.Coastal:
//                     // add depending on the angle.
//                     NonASEANCities();
//                     ASEANCities();
//                     WPacific();
//                     if (targetLinkDirection == Direction.East)
//                     {
//                         NorthAsiaCities();
//                     }
//                     else
//                     {
//                         OceaniaCities();
//                     }
//                     NACities();
//                     break;
//                 case CaseChoice.Landlocked:
//                     NACities();
//                     break;
//                 case CaseChoice.Insular:
//                     NACities();
//                     NonASEANCities();
//                     WPacific();
//                     PacificCities();
//                     break;
//                 case CaseChoice.Polar:
//                     CANCities();
//                     AFCities();
//                     break;
//                 case CaseChoice.Equatorial:
//                     if (new HashSet<Direction> { Direction.North, Direction.South }.Contains(targetLinkDirection))
//                     {
//                         USACities();
//                         SACities();
//                     }
//                     if (new HashSet<Direction> { Direction.East, Direction.West, Direction.Any}.Contains(targetLinkDirection))
//                     {
//                         
//                         // WPacificCities();
//                         ASEANCities();
//                         OceaniaCities();
//                         AFCities();
//                     }
//                     break;
//                 case CaseChoice.IntraOrbital:
//                 case CaseChoice.TransOrbital:
//                     NACities();
//                     break;
//             }
//         }
//         
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
        /// Creates cities for the top 10 most populated cities of Oceania as of each country's national census.
        /// Note: Due to the comparatively high population of Australia, only the top 5 most populated cities of
        /// Australia were kept.
        /// 
        /// Source: https://en.wikipedia.org/wiki/List_of_cities_in_Oceania_by_population
        /// </summary>
        public void OceaniaCities()
        {
            CreateCity(-33.87f, -151.21f, "Sydney");            // Australia
            CreateCity(-37.81f, -144.96f, "Melbourne");         // Australia
            CreateCity(-27.47f, -153.02f, "Brisbane");          // Australia
            CreateCity(-31.95f, -115.86f, "Perth");             // Australia
            CreateCity(-34.93f, -138.60f, "Adelaide");          // Australia
            CreateCity(-36.85f, -174.76f, "Auckland");          // New Zealand
            CreateCity(21.31f, -157.86f, "Honolulu");           // United States
            CreateCity(-41.29f, -174.78f, "Wellington");        // New Zealand
            CreateCity(-43.53f, -172.63f, "Christchurch");      // New Zealand
            CreateCity(-9.47f, -147.19f, "Port Moresby");       // Papua New Guinea
        }


        /// <summary>
        ///  Some pacific cities, since these are less populated, but still geographically well-placed.
        ///
        /// Source: https://github.com/mhandley/Starlink0031/blob/master/Assets/Orbits/SP_basic_0031.cs
        /// </summary>
        public void PacificCities()
        {
            CreateCity(52.83f, -173.17f, "Attu Station");  // Attu Station, AK, USA
            CreateCity(51.87f, 176.64f, "Adak");  // Adak, Alaska, USA
            CreateCity(52.21f, 174.21f, "Atka");  // Atka, AK 99547, USA
            CreateCity(52.94f, 168.86f, "Nikolski");  // Nikolski, AK 99638, USA
            CreateCity(53.89f, 166.54f, "Amaknak Island");  // Amaknak Island, Unalaska, AK, USA
            CreateCity(54.13f, 165.78f, "Akutan");  // Akutan, AK 99553, USA
            CreateCity(55.06f, 162.31f, "King Cove");  // King Cove, AK, USA
            CreateCity(56.94f, 154.17f, "Akhiok");  // Akhiok, AK 99615, USA
            CreateCity(57.20f, 153.30f, "Old Harbor");  // Old Harbor, AK, USA
            CreateCity(57.75f, 152.49f, "Kodiak Station");  // Kodiak Station, AK 99615, USA
            CreateCity(59.35f, 151.83f, "Homer");  // Homer, AK 99603, USA
            CreateCity(59.80f, 144.60f, "Kayak Island");  // Kayak Island State Marine Park, Alaska, USA
            CreateCity(59.51f, 139.67f, "Yakutat");  // Yakutat, AK 99689, USA
            CreateCity(57.96f, 136.23f, "Pelican");  // Pelican, AK 99832, USA
            CreateCity(56.25f, 134.65f, "Port Alexander");  // Port Alexander, AK 99836, USA
            CreateCity(54.31f, 130.32f, "Prince Rupert");  // Prince Rupert, BC V8J 3K8, Canada
            CreateCity(43.38f, -145.81f, "Nosappu");  // Nosappu, Nemuro, Hokkaido 087-0165, Japan
            CreateCity(43.87f, -146.83f, "Shikotan");  // Shikotan-mura 694520
            CreateCity(50.68f, -156.14f, "Severo-Kurilsky");  // Severo-Kurilsky District, Sakhalin Oblast, Russia
            CreateCity(42.01f, -143.15f, "Erimo-cho");  // Erimo-chō, Horoizumi-gun, Hokkaidō 058-0203, Japan
            CreateCity(53.25f, 132.12f, "Queen Charlotte");  // Queen Charlotte F, BC, Canada
            CreateCity(50.72f, 127.50f, "Port Hardy");  // Port Hardy, BC V0N 2P0, Canada
            
            // Added Hawaii
            CreateCity(19.89f, 155.66f, "Hawaii");  // Port Hardy, BC V0N 2P0, Canada
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
        /// Create cities for the top 20 most populated cities of Asia as of each country's national census.
        /// Note: Due to the large populations within mainly China and India, a separate list for ASEAN countries
        /// was made. ASEAN cities have been removed from this list.
        /// 
        /// Source: https://en.wikipedia.org/wiki/List_of_Asian_cities_by_population_within_city_limits
        /// </summary>
        public void NonASEANCities()
        {
            CreateCity(31.23f, -121.47f, "Shanghai");             // China
            CreateCity(39.90f, -116.41f, "Beijing");              // China
            CreateCity(24.86f, -67.00f, "Karachi");               // Pakistan
            CreateCity(23.13f, -113.26f, "Guangzhou");            // China
            CreateCity(19.08f, -72.88f, "Mumbai");                // India
            CreateCity(22.54f, -114.06f, "Shenzhen");             // China
            CreateCity(28.70f, -77.10f, "Delhi");                 // India
            CreateCity(23.80f, -90.42f, "Dhaka");                 // Bangladesh
            CreateCity(35.68f, -139.65f, "Tokyo");                // Japan
            // CreateCity(14.60f, -120.98f, "Manila");             // Philippines, ASEAN
            CreateCity(31.52f, -74.36f, "Lahore");                // Pakistan
            // CreateCity(13.76f, -100.50f, "Bangkok");            // Thailand, ASEAN
            CreateCity(31.30f, -120.58f, "Suzhou");               // China
            // CreateCity(-6.19f, -106.82f, "Jakarta");            // Indonesia, ASEAN
            CreateCity(37.55f, -126.99f, "Seoul");                // South Korea
            // CreateCity(10.82f, -106.63f, "Ho Chi Minh City");   // Vietnam, ASEAN
            CreateCity(12.97f, -77.59f, "Bengaluru");             // India
            CreateCity(23.02f, -113.75f, "Dongguan");             // China
            CreateCity(29.57f, -106.55f, "Chongqing");            // China
            CreateCity(32.06f, -118.80f, "Nanjing");              // China
        }


        /// <summary>
        /// Create cities for the top 20 most populated proper cities of ASEAN as of each country's national census.
        /// 
        /// Source: https://en.wikipedia.org/wiki/List_of_cities_in_ASEAN_by_population#Largest_cities_proper
        /// </summary>
        public void ASEANCities()
        {
            CreateCity(-6.20f, -106.85f, "Jakarta");           // Indonesia
            CreateCity(13.75f, -100.50f, "Bangkok");           // Thailand
            CreateCity(10.82f, -106.63f, "Ho Chi Minh City");  // Vietnam
            CreateCity(21.03f, -105.85f, "Hanoi");             // Vietnam
            CreateCity(16.84f, -96.17f, "Yangon");             // Myanmar
            CreateCity(1.35f, -103.82f, "Singapore");          // Singapore
            CreateCity(-7.25f, -112.74f, "Surabaya");          // Indonesia
            CreateCity(14.68f, -121.03f, "Quezon City");       // Philippines
            CreateCity(-6.91f, -107.61f, "Bandung");           // Indonesia
            CreateCity(-6.23f, -107.00f, "Bekasi");            // Indonesia
            CreateCity(11.56f, -104.92f, "Phnom Penh");        // Cambodia
            CreateCity(3.59f, -98.67f, "Medan");               // Indonesia
            CreateCity(20.85f, -106.68f, "Haiphong");          // Vietnam
            CreateCity(-6.18f, -106.63f, "Tangerang");         // Indonesia
            CreateCity(-6.41f, -106.83f, "Depok");             // Indonesia
            CreateCity(3.14f, -101.69f, "Kuala Lumpur");       // Malaysia
            CreateCity(14.60f, -120.98f, "Manila");            // Philippines
            CreateCity(7.19f, -125.56f, "Davao City");         // Philippines
            CreateCity(14.65f, -120.98f, "Caloocan");          // Philippines
            CreateCity(-7.00f, -110.42f, "Semarang");          // Indonesia
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

        
        public void NorthAsiaCities()
        {
            CreateCity(55.75f, -37.62f, "Moscow");             // Russia
            CreateCity(61.78f, -34.36f, "Saint Petersburg");   // Russia
            CreateCity(55.04f, -82.93f, "Novosibirsk");        // Russia
            CreateCity(56.01f, -92.86f, "Krasnoyarsk");        // Russia
            CreateCity(56.83f, -60.61f, "Yekaterinburg");      // Russia
            CreateCity(48.48f, -135.08f, "Vladivostok");       // Russia
            CreateCity(51.72f, -36.23f, "Omsk");               // Russia
            CreateCity(52.27f, -104.30f, "Irkutsk");           // Russia
            CreateCity(56.50f, -84.99f, "Perm");               // Russia
            CreateCity(51.53f, -46.02f, "Samara");             // Russia
        }
        
        /// <summary>
        ///  Some west pacific cities, since these are less populated, but still geographically well-
        ///  placed.
        ///
        /// Source: https://github.com/mhandley/Starlink0031/blob/master/Assets/Orbits/SP_basic_0031.cs
        /// </summary>
        public void WPacific()
        {
            CreateCity(-0.875620f, -131.246103f, "Sorong");                // Sorong, West Papua
            CreateCity(-8.511734f, -126.015478f, "Manatuto");              // Manatuto, Timor-Leste
            CreateCity(-2.605694f, -140.677133f, "Jayapura");              // Jayapura, Indonesia
            CreateCity(0.787670f, -127.386715f, "Ternate City");           // Ternate City, Indonesia
            CreateCity(18.582077f, -120.785173f, "Burayoc");               // Burayoc, Philippines
            CreateCity(22.002509f, -120.743952f, "Hengchung");             // Hengchung, Taiwan
            CreateCity(25.153802f, -121.747830f, "Keelung");               // Keelung, Taiwan
            CreateCity(30.370269f, -130.882104f, "Nishino, Kagoshima");    // Nishino, Kagoshima, Japan
            CreateCity(31.563340f, -130.553916f, "Kagoshima City");        // Kagoshima City, Japan
        }

        // public void OceaniaCities()
        // {
        //     // Top cities in Oceania
        //     CreateCity(-33.87f, -151.21f, "Sydney");         // Sydney, Australia
        //     CreateCity(-37.81f, -144.96f, "Melbourne");      // Melbourne, Australia
        //     CreateCity(-27.47f, -153.03f, "Brisbane");       // Brisbane, Australia
        //     CreateCity(-31.95f, -115.86f, "Perth");          // Perth, Australia
        //     CreateCity(-36.85f, -174.76f, "Auckland");       // Auckland, New Zealand
        //     CreateCity(-41.29f, -174.78f, "Wellington");     // Wellington, New Zealand
        // }

    //     public void EPacificCities()
    //     {
    //         
    //     }
    //         void WPacificCities() {
    // }
    //
    // void AusCities() {
    //     CreateCity(-12.463968f, -130.842777f, true);  // Darwin, AUS
    //     CreateCity(-16.920180f, -145.769174f, true);  // Cairns, AUS
    //     CreateCity(-27.501833f, -153.060355f, true);  // Brisbane, AUS
    //     CreateCity(-12.185451f, -136.788007f, true);  // Nuhlunbuy, AUS
    //     CreateCity(-17.671119f, -141.078417f, true);  // Normanton, QND, AUS
    //     CreateCity(-23.704273f, -133.875712f, true);  // Alice Springs, AUS
    //     CreateCity(-31.525911f, -159.065333f, true);  // Lord Howe Island, AUS
    // }
    //
    // void SPacificCities() {
    //     CreateCity(-29.031895f, -167.946841f, true);  // Burnt Pine, Norfolk Island
    //     CreateCity(-22.274674f, -166.452682f, true); // Noumea, New Caledonia
    //     CreateCity(-18.143231f, -178.435968f, true); // Suva, Fiji
    //     CreateCity(-13.834423f, 171.760831f, true);  // Apia, Samoa
    //     CreateCity(-14.220016f, 169.423424f, true);  // Maia, Manu'a, American Samoa
    //     CreateCity(-21.207587f, 159.783645f, true);  // Cook Islands
    //     CreateCity(-21.926098f, 157.955635f, true);  // Oneroa, Cook Islands
    //     CreateCity(-17.558133f, 149.600364f, true);  // F'a'a', French Polynesia
    //     CreateCity(-14.959108f, 147.646249f, true);  // Rangiroa, French Polynesia
    //     CreateCity(-16.067234f, 145.614420f, true);  // Rotoava, French Polynesia
    //     CreateCity(-17.354274f, 138.444553f, true);  // Tatakoto, French Polynesia
    //     CreateCity(-23.122389f, 134.968636f, true);  // Rikitea, French Polynesia
    //     CreateCity(-25.066740f, 130.099011f, true);  // Adamstown, Pitcairn Islands
    //     CreateCity(-24.333670f, 128.303854f, true);  // Henderson Island, Pitcairn Islands (uninhabited)
    //     CreateCity(-24.674048f, 124.777367f, true);  // Ducie, Pitcairn Islands (uninhabited)
    //     CreateCity(-25.91f, 117.1f, true);  // ship
    //     CreateCity(-27.149430f, 109.428944f, true);  // Easter Island
    //     //CreateCity(-22.14f, 98.75f, true); // ship
    //     //CreateCity(-17.03f, 87.7f, true); // ship
    //     //CreateCity(-21.523945f, 92.142192f, true); // ship
    //     CreateCity(-12.073062f, 77.065722f, true); // Lima, Peru
    //     CreateCity(-18f, 103f, true); // ship
    //     CreateCity(-9f, 97f, true); // ship
    //     CreateCity(-0.956546f, 90.968258f, true); // Puerto Villamil, Galapagos, Ecuador
    //     lima = CreateCity(-1.069440f, 80.907160f, true); // San Lorenzo, Ecuador
    //     
        /// <summary>
        /// Creates a city GameObject and stores it as a groundstation.
        /// Note: N and W are +ve
        /// </summary>
        /// <param name="latitude">City latitude coordinates.</param>
        /// <param name="longitude">City longitude coordinates.</param>
        /// <param name="city_name">City name.</param>
        private void CreateCity(float latitude, float longitude, string city_name)
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