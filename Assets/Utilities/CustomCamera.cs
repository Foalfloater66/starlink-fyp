using System;
using System.Collections.Generic;
using System.Data.OleDb;
using UnityEngine;
using Attack;
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
		Vector3 lightrot;
		int cam_count;
		int current_cam;
		bool pause = false;
		float pause_start_time;

		[FormerlySerializedAs("attackArea")] [FormerlySerializedAs("attack_choice")] [HideInInspector] public QualitativeCase qualitativeCase; // TODO: Revisit this. Does it get updated?
		
		public Light sun;
		float FoV = 60f;
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
			initialized = true;
		}

		private void _ViewUS()
		{
			// North America View
			cam_count = 2;

			void PositionCamera()
			{
				positions.Add(new Vector3(-25f, 23f, -8f));
				angles.Add(new Vector3(45f, 70f, 0f));
			}

			PositionCamera();
			times.Add (0f);
			speeds.Add (0.01f);
			
			PositionCamera();
			times.Add (20f);
			speeds.Add (0.11f);

			PositionCamera();
			times.Add (100000f);
			speeds.Add (0.01f);
			
			lightrot = new Vector3 (20f, 130f, 0f);
			FoV = 60f;
		}
		
		private void _ViewCoastalUS()
		{
			// North America View
			cam_count = 2;

			void PositionCamera()
			{
				positions.Add(new Vector3(-25f, 18f, 0f));
				angles.Add(new Vector3(20f, 90f, 0f));
			}

			PositionCamera();
			times.Add (0f);
			speeds.Add (0.01f);
			
			PositionCamera();
			times.Add (20f);
			speeds.Add (0.11f);

			PositionCamera();
			times.Add (100000f);
			speeds.Add (0.01f);
			
			lightrot = new Vector3 (20f, 130f, 0f);
			FoV = 90f;
		}
		
		private void _ViewPolar()
		{
			cam_count = 2;

			void PositionCamera()
			{
				positions.Add(new Vector3(-18f, 20f, -18f));
				angles.Add(new Vector3(35f, 47f, 0f));
			}

			PositionCamera();
			times.Add (0f);
			speeds.Add (0.01f);
			
			PositionCamera();
			times.Add (20f);
			speeds.Add (0.11f);

			PositionCamera();
			times.Add (100000f);
			speeds.Add (0.01f);
			
			lightrot = new Vector3 (20f, 130f, 0f);
			FoV = 85f;
		}
		
		private void _ViewAmericanEquator()
		{
			// Equator View 
			cam_count = 2;

			void PositionCamera()
			{
				positions.Add(new Vector3(-25f, 10f, -22f));
				angles.Add(new Vector3(15, 50f, 0f));
			}

			PositionCamera();
			times.Add (0f);
			speeds.Add (0.01f);
			
			PositionCamera();
			times.Add (20f);
			speeds.Add (0.11f);

			PositionCamera();
			times.Add (100000f);
			speeds.Add (0.01f);
			
			lightrot = new Vector3 (-5f, 20f, 20f);
			FoV = 80f;
		}

		public void InitView() {
			Start ();
			switch (qualitativeCase)
			{
				case QualitativeCase.Polar:
					_ViewPolar();
					break;
				case QualitativeCase.SimpleDemo:
				case QualitativeCase.Landlocked:
					_ViewUS(); // TODO: change this to have the coast in centreview. 
					break;
				case QualitativeCase.Coastal:
					_ViewCoastalUS();
					break;
				case QualitativeCase.Equatorial:
					_ViewAmericanEquator();
					break;
				case QualitativeCase.IntraOrbital: 
				// TODO: got to do this at another point.
				case QualitativeCase.TransOrbital:
					// TODO: find something that would work here.
				default:
					_ViewUS();
					break;
			}
			sun.transform.rotation = Quaternion.Euler (lightrot);
			Camera.main.fieldOfView = FoV;
			
			current_cam = 0;

			if (positions.Count > 0) {
				transform.position = positions[0] * scale;
				transform.rotation = Quaternion.Euler (angles [0]);
			}

			start_time = Time.time;
		}
	
		// Update is called once per frame
		void Update () {
			if (Input.GetKeyDown ("space") || Input.GetKeyDown (".")) {
				if (pause == false) {
					pause = true;
					pause_start_time = Time.time;
					print ("space key was pressed");
				} else {
					pause = false;
					start_time += Time.time - pause_start_time;
					print ("space key was pressed");
				}
			}
			
			// test code from chatgpt
			// Camera cam = GetComponent<Camera>();
			// float screenAspect = (float)Screen.width / (float)Screen.height;
			// float targetAspect = 1.0f;  // Square aspect ratio
			//
			// float scaleHeight = screenAspect / targetAspect;
			// Rect rect = cam.rect;
			//
			// if (scaleHeight < 1.0f)
			// {
			// 	rect.width = 1.0f;
			// 	rect.height = scaleHeight;
			// 	rect.x = 0;
			// 	rect.y = (1.0f - scaleHeight) / 2.0f;
			// }
			// else
			// {
			// 	float scaleWidth = 1.0f / scaleHeight;
			// 	rect.width = scaleWidth;
			// 	rect.height = 1.0f;
			// 	rect.x = (1.0f - scaleWidth) / 2.0f;
			// 	rect.y = 0;
			// }
			//
			// cam.rect = rect;
		}

		// Update is called once per frame
		void LateUpdate () {
			Vector3 pos1, pos, pdiff;
			Vector3 rot1, cur_rot, rot, rdiff;
			if (pause || positions.Count == 0) {
				return;
			}
			// Have we passed the next time?
			if (Time.time - start_time > times [current_cam + 1]) {
				current_cam++;
				cam_count--;
			}
			// f goes from zero at times[0] to one at times[1]
			float elapsed_time = Time.time - start_time;
			float f = (elapsed_time - times [current_cam]) / (times [current_cam + 1] - times [current_cam]);
			pos1 = Vector3.Lerp (positions [current_cam]*scale, positions [current_cam + 1]*scale, f);
			pdiff = pos1 - transform.position;
			pos = transform.position + speeds[current_cam + 1] * pdiff;
			rot1 = Vector3.Lerp (angles [current_cam], angles [current_cam + 1], f);
			cur_rot = transform.rotation.eulerAngles;
			rdiff = rot1 - cur_rot;
			if (rdiff.x < -180f) {
				rdiff.x += 360f;
			}
			if (rdiff.y < -180f) {
				rdiff.y += 360f;
			}
			if (rdiff.z < -180f) {
				rdiff.z += 360f;
			}
			rot = cur_rot + speeds[current_cam + 1] * rdiff;

			transform.position = pos;
			transform.rotation = Quaternion.Euler(rot);

		}
	}
}
