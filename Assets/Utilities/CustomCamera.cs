using System.Collections.Generic;
using Attack.Cases;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Utilities
{
	public class CustomCamera : MonoBehaviour {
		float timer;
		float start_time;
		List <Vector3> positions;
		List<Vector3> angles;
		List<float> times;
		List<float> speeds;
		private List<float> FoVs;
		private List<Vector3> lightrots;
		public int cam_count { get; private set; }
		int current_cam;
		float pause_start_time;

		[FormerlySerializedAs("qualitativeCase")] [FormerlySerializedAs("attackArea")] [FormerlySerializedAs("attack_choice")] [HideInInspector] public CaseChoice caseChoice; 
		
		public Light sun;
		bool initialized = false;
		float scale = 316f;  // scale from old coordinate system to new km-based one

		// Use this for initialization
		void Start () {
			if (initialized) // don't initialize twice
				return;
			positions = new List<Vector3> ();
			angles = new List<Vector3> ();
			times = new List<float> ();
			speeds = new List<float> ();
			FoVs = new List<float>();
			lightrots = new List<Vector3>();
			initialized = true;
		}

		/// <summary>
		/// Camera view for the qualitative Landlocked and Demo examples.
		/// </summary>
		public void ViewLandlocked()
		{
			cam_count = 1;

			// North America View
			positions.Add(new Vector3(-25f, 23f, -8f));
			angles.Add(new Vector3(45f, 70f, 0f));
			times.Add (0f);
			speeds.Add (0.01f);
			FoVs.Add(60f);
			
			lightrots.Add(new Vector3 (20f, 130f, 0f));
		}

		/// <summary>
		/// Camera views for the qualitative Coastal example.
		/// </summary>
		public void ViewCoastal(Direction targetLinkDirection)
		{
			cam_count = 2;

			// America view
			positions.Add(new Vector3(-30f, 18f, -3f)); 
			angles.Add(new Vector3(30f, 90f, 0f));
			times.Add (0f);
			speeds.Add (0.01f);
			FoVs.Add(60f);
			lightrots.Add(new Vector3 (20f, 130f, 0f));
			
			if (targetLinkDirection == Direction.West)
			{
				// New Zealand view
				positions.Add(new Vector3(-6.3f, -12.6f, 37.9f));
				angles.Add(new Vector3(-20f, 170f, 0f));
				times.Add (0f);
				speeds.Add (0.01f);
				FoVs.Add(60f);
				lightrots.Add(new Vector3(-20f, 170f, 0f));
			} 
			else if (targetLinkDirection == Direction.East)
			{
				// North Asia view
				positions.Add(new Vector3(-9f, 26f, 25f));
				angles.Add(new Vector3(40f, 160f, 0f));
				times.Add (0f);
				speeds.Add (0.01f);
				FoVs.Add(60f);
				lightrots.Add(new Vector3(40f, 160f, 0f));
			}
			else
			{
				// Asia view
				positions.Add(new Vector3(15.8f, 0f, 37.9f));
				angles.Add(new Vector3(0f, -150f, 0f));
				times.Add (0f);
				speeds.Add (0.01f);
				FoVs.Add(60f);
				lightrots.Add(new Vector3(0f, -150f, 0f));
			}
		}
		
		/// <summary>
		/// Camera views for the qualitative Coastal example.
		/// </summary>
		public void ViewInsular()
		{
			cam_count = 3;

			// America view
			positions.Add(new Vector3(-30f, 18f, -3f)); 
			angles.Add(new Vector3(30f, 90f, 0f));
			times.Add (0f);
			speeds.Add (0.01f);
			FoVs.Add(60f);
			lightrots.Add(new Vector3 (20f, 130f, 0f));
			
				// Middle Pacific view
				positions.Add(new Vector3(-25f, 10f, 30f));
				angles.Add(new Vector3(10f, 135f, 0f));
				times.Add (0f);
				speeds.Add (0.01f);
				FoVs.Add(60f);
				lightrots.Add(new Vector3(40f, 160f, 0f));
				
				// Oceania view
				positions.Add(new Vector3(-6.3f, -12.6f, 37.9f));
				angles.Add(new Vector3(-20f, 170f, 0f));
				times.Add (0f);
				speeds.Add (0.01f);
				FoVs.Add(60f);
				lightrots.Add(new Vector3(-20f, 170f, 0f));
		}

		/// <summary>
		/// Camera view for the illustrative Polar example.
		/// </summary>
		public void ViewPolar()
		{
			cam_count = 1;

			positions.Add(new Vector3(-18f, 20f, -18f));
			angles.Add(new Vector3(35f, 47f, 0f));
			times.Add (0f);
			speeds.Add (0.01f);
			
			lightrots.Add(new Vector3 (20f, 130f, 0f));
			FoVs.Add(85f);			
		}

		/// <summary>
		/// Camera view for the illustrative Equator example.
		/// </summary>
		public void ViewAmericanEquator()
		{
			cam_count = 1;

			positions.Add(new Vector3(-25f, 10f, -22f));
			angles.Add(new Vector3(15, 50f, 0f));
			times.Add (0f);
			speeds.Add (0.01f);
			lightrots.Add(new Vector3 (-5f, 20f, 20f));
			FoVs.Add(80f);
		}

		public void SetupView()
		{
			Start();
		}

		/// <summary>
		/// Initialize the view for the specified qualitative case.
		/// </summary>
		public void InitView() {
			sun.transform.rotation = Quaternion.Euler (lightrots[0]);
			current_cam = 0;
			transform.position = positions[0] * scale; 
			transform.rotation = Quaternion.Euler (angles [0]);
			Assert.IsNotNull(Camera.main );
			Camera.main.fieldOfView = FoVs[0];
			start_time = Time.time;
		}
	
		/// <summary>
		/// Switch between the views available for the given qualitative example.
		/// </summary>
		public void SwitchCamera()
		{
			current_cam = (current_cam + 1) % cam_count;
			transform.position = positions[current_cam]*scale;
			transform.rotation = Quaternion.Euler (angles [current_cam]);
			Camera cam = Camera.main;
			Assert.IsNotNull(cam);
			cam.fieldOfView = FoVs[current_cam];
			sun.transform.rotation = Quaternion.Euler (lightrots[current_cam]);
		}		
	}
}
