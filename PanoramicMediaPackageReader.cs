using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Threading;
using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine.Networking;
using XR.External;
using XR.General;
using UnityEngine.Video;

/// <summary>
/// This class reads a CSV of panoramic media and data relating to the media and uses it to create A series of Points that can be used to view the Media Immersivley
/// </summary>
public class PanoramicZipPackageReader : MonoBehaviour
{
    public static PanoramicZipPackageReader Instance;

    #region Unity Editor Variables
    [SerializeField] GameObject _panoramicSpherePrefab = null;
    #endregion

    #region Private Variables
    Transform _cameraRig = null;
    const char LINE_SEPERATOR = '\n';
    const char VALUE_SEPERATOR = ' ';

    bool _cancelFileParse = false;
    string licenseError = "";
    List<Point> _convertedPoints = null;
    List<PanoramicSphere> _panoramicPoints = null;
    List<PanoramicMediaSphereBehaviour> _panoramicMediaSphereBehaviours = null;
    List<Material> _materials = null;
    List<string> _imageNames = null;
    #endregion //Private Variables

    #region Public Properties
    public Texture2D CurrentTexture { get; set; }
    public GameObject CurrentSphere { get; set; }
    public Transform CameraRig { get { return _cameraRig; } }
    #endregion //Public Properties

    #region Public Methods
    public void StartFileRead(string fullFilePath)
	{
		Debug.LogWarning("FullPath = " + fullFilePath);
		ExtractImageFolder(fullFilePath);
	}

	public void FileParse(string fullPath)
	{
        Debug.Log("Path = " + fullPath);
		string folderPath = Path.GetDirectoryName(fullPath);

		LinesLoaded = true;

		StreamReader streamReader = new StreamReader(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
		string line;

		streamReader.ReadLine(); // skip first line
		int i = 1;
        PanoramicSphere.MediaType type = PanoramicSphere.MediaType.Image;
        while (!_cancelFileParse && (line = streamReader.ReadLine()) != null)
		{
			KeyValuePair<Vector3, string> nameAndPos;
			string[] values = line.Split('\t', ',');
            PanoramicSphere temporaryPointClass = new PanoramicSphere();
            if (values.Length == 9 && values[8].Length > 0)
            {
				try
				{
                    switch(values[1])
                    {
                        case "IMAGE":
                            type = PanoramicSphere.MediaType.Image;
                            break;
                        case "VIDEO":
                            type = PanoramicSphere.MediaType.Video;
                            break;
                    }

                    float x = 0;
                    float y = 0;

                    if (float.TryParse(values[2],out x) && float.TryParse(values[3],out y))
					{
						Vector3 position = new Vector3(x,0,y);
                        string imagePath = values[8].Replace("files/", "");
                        imagePath = imagePath.Replace("/", "\\");
                        string mediaFolderName = "Images";
                        string mediaFolderPath = imagePath; // Path.Combine(mediaFolderName, imagePath);
                        string fullMediaPath = Path.Combine(folderPath, mediaFolderPath);
						Debug.Log("Media file path = " + fullMediaPath);
						if (File.Exists(Path.Combine(folderPath, mediaFolderPath)))
						{
							if (values[1] == "") // No Name Found
							{
								nameAndPos = new KeyValuePair<Vector3, string>(position, string.Format("No Media name on Line #{0}", i.ToString()));
							}
							else
								nameAndPos = new KeyValuePair<Vector3, string>(position, values[1]);

                            temporaryPointClass.FileName = fullMediaPath;
						}
						else
						{
							Debug.LogFormat("Couldn't find Media #{0}", i.ToString());
							nameAndPos = new KeyValuePair<Vector3, string>(position, string.Format("Couldn't find Media #{0}", i.ToString()));
                            temporaryPointClass.FileName = string.Format("Line {0} is erroneous. Could Not Find File", i.ToString());
                        }
                    }
					else //Position failed to parse
					{
						Debug.LogFormat("Could not read position #{0}", i.ToString());
						Vector3 position = new Vector3(i, i, i);
						nameAndPos = new KeyValuePair<Vector3, string>(position, string.Format("Could not read position #{0}", i.ToString()));
                        temporaryPointClass.FileName = string.Format("Line {0} is erroneous. Position Could not be read", i.ToString());
                    }


                }
				catch
				{
					Debug.LogFormat("Could not read line #{0} Please check format", i.ToString());
					Vector3 position = new Vector3(i, i, i);
					nameAndPos = new KeyValuePair<Vector3, string>(position, string.Format("Line {0} is erroneous.", i.ToString()));
                    temporaryPointClass.FileName = string.Format("Line {0} is erroneous.", i.ToString());
                }
            }
			else
			{
				Debug.LogFormat("Incorrect entry count in Line #{0}.", i.ToString());
				Vector3 position = new Vector3(i, i, i);
				nameAndPos = new KeyValuePair<Vector3, string>(position, string.Format("Incorrect entry count in Line #{0}.", i.ToString()));
                temporaryPointClass.FileName = string.Format("Line {0} is erroneous. Incorrect Line Length Too Many Entries", i.ToString());
            }
            temporaryPointClass.Position = nameAndPos.Key;
            temporaryPointClass.Name = nameAndPos.Value;
            temporaryPointClass.Type = type;
            _panoramicPoints.Add(temporaryPointClass);
            i++;
		}

        streamReader.Dispose();
		if (_cancelFileParse)
		{
			Debug.LogWarning("FileParse while loop was cancelled");
		}
		CreateSpheres();
	}

	public void ChangeSkybox (int value)
	{
		_panoramicMediaSphereBehaviours[value].DropDownValuechanged();
	}
    #endregion //Public Methods

    #region Private Methods
    /// <summary>
    /// Extract a media package from the designated zip path. Begin reading the CSV
    /// </summary>
    /// <param name="path"></param>
    void ExtractImageFolder(string path)
	{
		TemporaryFileManager _cacheManager = XRUtilities.Get<TemporaryFileManager>();
		ZipFile zip = new ZipFile(path);

		string mediaPackageName = Path.GetFileNameWithoutExtension(path);
		string folderPath = Path.Combine(_cacheManager.CachePath, mediaPackageName);
        zip.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
		zip.ExtractAll(folderPath);
        zip.Dispose();
		string fullFilePath = "";

		string[] files = Directory.GetFiles(folderPath, "*.csv", SearchOption.AllDirectories);
		if (files != null && files.Length > 0)
			fullFilePath = files[0];
		else
			Debug.LogError("Unable to locate file matching pattern '*.csv' in folder: " + folderPath);

		if (File.Exists(fullFilePath))
			FileParse(fullFilePath);
		else
			Debug.LogError("No File exists at path: " + fullFilePath);
	}

    /// <summary>
    /// Parse all MediaSphere data from the extracted CSV. Generate Panoramic media spheres and then compile a list of all available spheres.
    /// Lastly create Teleportable points for 2D UI interaction.
    /// </summary>
	void CreateSpheres()
	{
		int index = 0;
		bool firstImageFound = true;
		PanoramicMediaSphereBehaviour firstCorrectPath = null;

        foreach (PanoramicSphere panoramicPoint in _panoramicPoints)
        {
            string mediaName = panoramicPoint.Name;
            string mediaPath = panoramicPoint.FileName;

            Transform sphere = GameObject.Instantiate(_panoramicSpherePrefab).transform;
            sphere.position = panoramicPoint.Position;

            PanoramicMediaSphereBehaviour _panoramicMediaSphereBehaviour = sphere.gameObject.AddComponent<PanoramicMediaSphereBehaviour>();
            _panoramicMediaSphereBehaviour.FileRead = this;
            _panoramicMediaSphereBehaviour.Index = index;
            _panoramicMediaSphereBehaviour.ImagePath = mediaPath;
            string ext = Path.GetExtension(mediaPath);

            switch (ext)
            {
                case ".mp4":
                case ".asf":
                case ".avi":
                case ".dv":
                case ".m4v":
                case ".mov":
                case ".mpg":
                case ".mpeg":
                case ".ogv":
                case ".vp8":
                case ".webm":
                case ".wmv":
                    panoramicPoint.Type = PanoramicSphere.MediaType.Video;
                    break;
            }

            if (panoramicPoint.Type == PanoramicSphere.MediaType.Image)
            {
                _panoramicMediaSphereBehaviour.Type = PanoramicSphere.MediaType.Image;
                Shader skyboxShader = Shader.Find("Skybox/Panoramic");
			    Material skyBox = new Material(skyboxShader);
                skyBox.name = mediaName;
                RenderSettings.skybox = skyBox;
                _materials.Add(skyBox);
                _panoramicMediaSphereBehaviour.PanoramicImage = skyBox;
            }
            else if(panoramicPoint.Type == PanoramicSphere.MediaType.Video)
            {
                _panoramicMediaSphereBehaviour.Type = PanoramicSphere.MediaType.Video;
            }

            _panoramicMediaSphereBehaviours.Add(_panoramicMediaSphereBehaviour);

            index++;

            _cameraRig.transform.position = panoramicPoint.Position;

			if (panoramicPoint.FileName.Contains("erroneous"))
			{
				sphere.gameObject.SetActive(false);
			}
			else
			{
				Point thisPoint = new Point(mediaName, Path.GetFileNameWithoutExtension(mediaPath),panoramicPoint.Position, index);
				_convertedPoints.Add(thisPoint);
			}

			if (firstImageFound && File.Exists(mediaPath))
			{
				firstImageFound = false;
				firstCorrectPath = _panoramicMediaSphereBehaviour;
			}
		}

        //Use the first successfully parsed piece of media as the inital entry point for the experience
		if (firstCorrectPath != null)
			firstCorrectPath.LoadMedia();

	}
    #endregion //Private Methods

    #region Unity Methods
    /// <summary>
    /// Initialise all collections and establish the instance.
    /// </summary>
    private void Awake()
	{
        if (Instance == null)
            Instance = this;
        else
            return;
		_cameraRig = XRUserManager.Instance.CurrentXRUser;
		_panoramicMediaSphereBehaviours = new List<PanoramicMediaSphereBehaviour>();
        _panoramicPoints = new List<PanoramicSphere>();
		_materials = new List<Material>();
		_imageNames = new List<string>();
	}

    /// <summary>
    /// Poll continously to determine if the Raycast is hitting a media sphere
    /// </summary>
	private void Update()
	{
		if (RayCastController.Instance.IsHit && RayCastController.Instance.HitInfo.collider != null)
		{
			if (RayCastController.Instance.HitInfo.collider.GetComponent<PanoramicMediaSphereBehaviour>() != null)
			{
                PanoramicMediaSphereBehaviour currentHoveredWaypoint = RayCastController.Instance.HitInfo.collider.GetComponent<PanoramicMediaSphereBehaviour>();
				if (XRInputManager.Instance.PrimaryTriggerDown)
					currentHoveredWaypoint.LoadMedia();

			}
		}
	}

    /// <summary>
    /// On destroy clean up all Panoramic media Spheres
    /// </summary>
	private void OnDestroy()
	{
		if(ApplicationManager.Instance.IsInPanoramic)
		{
			foreach(PanoramicMediaSphereBehaviour behaviour in _panoramicMediaSphereBehaviours)
			{
				GameObject.Destroy(behaviour.gameObject);
			}
		}
	}
    #endregion //Unity Methods

}