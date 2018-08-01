using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* import watson sdk related */
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.DataTypes;
using UnityEngine.UI;

public class WatsonHelvetica : MonoBehaviour {

    #region PLEASE SET THESE VARIABLES IN THE INSPECTOR
    [Space(10)]
    [Tooltip("The service URL (optional). This defaults to \"https://stream.watsonplatform.net/speech-to-text/api\"")]
    [SerializeField]
    public string _serviceUrl;
    [Tooltip("Text field to display the results of streaming.")]
    public Text ResultsField;
    [Header("CF Authentication")]
    [Tooltip("The authentication username.")]
    [SerializeField]
    public string _username;
    [Tooltip("The authentication password.")]
    [SerializeField]
    public string _password;
    [Header("IAM Authentication")]
    [Tooltip("The IAM apikey.")]
    [SerializeField]
    public string _iamApikey;
    [Tooltip("The IAM url used to authenticate the apikey (optional). This defaults to \"https://iam.bluemix.net/identity/token\".")]
    [SerializeField]
    public string _iamUrl;
    #endregion

    private int _recordingRoutine = 0;
    private string _microphoneID = null;
    private AudioClip _recording = null;
    private int _recordingBufferSize = 1;
    private int _recordingHZ = 22050;

    public SpeechToText _Watsonservice;

    [HideInInspector]
    public string text3DtoShow;

    #region PLEASE SET THESE VARIABLES IN THE INSPECTOR
    [Space(10)]
    [Header("Helvetica character")]
    /* text related*/
    [SerializeField]
    public string _TextToShow = "watson\nand \nhelvetica";
    [SerializeField]
    public float _CharacterSpacing = 4f;
    [SerializeField]
    public float _LineSpacing = 25f;
    [SerializeField]
    public float _SpaceWidth = 8f;

    #endregion

    private string _PrevFrameText = "watson\nand \nhelvetica";
    private float _PrevFrameCharacterSpacing = 4f;
    private float _PrevFrameLineSpacing = 25f;
    private float _PrevFrameSpaceWidth = 8f;
    private float _CharXLocation = 0f;
    private float _CharYLocation = 0f;
    private Vector3 _ObjScale; //the scale of the parent object

    public bool addRigibody = true;
    public bool addBoxCollider = true;
    public bool BoxColliderIsTrigger = false;
    private bool boxcollideradd = false;
    private bool rigibodyadd = false;

    //rigidbody variables
    public float Mass = 1f;
    public float Drag = 0f;
    public float AngularDrag = 0.05f;
    public bool UseGravity = true;
    public bool IsKinematic = false;
    public RigidbodyInterpolation Interpolation;
    public CollisionDetectionMode CollisionDetection;
    public bool FreezePositionX = false;
    public bool FreezePositionY = false;
    public bool FreezePositionZ = false;
    public bool FreezeRotationX = false;
    public bool FreezeRotationY = false;
    public bool FreezeRotationZ = false;
    // Use this for initialization
    void Start () {

        transform.Find("_Alphabets").gameObject.SetActive(false);
        /* start watson service */
        LogSystem.InstallDefaultReactors();
        Runnable.Run(CreateWatsonService());
        /* disable _Alphabets and all children under it to remove them from being seen. */
        // transform.Find("_Alphabets").gameObject.SetActive(false);
        _TextToShow = _PrevFrameText;
        _CharacterSpacing = _PrevFrameCharacterSpacing;
        _LineSpacing = _PrevFrameLineSpacing;
        _SpaceWidth = _PrevFrameSpaceWidth;
        text3DtoShow = _TextToShow;
        transform.Find("_Alphabets").gameObject.SetActive(false);
        GenerateText();
        boxcollideradd = false;
        rigibodyadd = false;
        addBoxCollider = true;
        addRigibody = true;
    }


    // Update is called once per frame
    void Update()
    {
        text3DtoShow = _TextToShow;
        if(_TextToShow!=_PrevFrameText||_CharacterSpacing!=_PrevFrameCharacterSpacing||_LineSpacing!=_PrevFrameLineSpacing||_SpaceWidth!=_PrevFrameSpaceWidth)
        {
            _PrevFrameText =_TextToShow;
            _PrevFrameCharacterSpacing = _CharacterSpacing;
            _PrevFrameLineSpacing = _LineSpacing;
            _PrevFrameSpaceWidth = _SpaceWidth;
            GenerateText();
        }
        if (addBoxCollider==true && boxcollideradd ==false)
        {
            foreach (Transform child in transform.Find("_Alphabets"))
            {
                child.gameObject.AddComponent<BoxCollider>();
            }

            foreach (Transform child in transform)
            {
                if (child.name != "_Alphabets")
                {
                    child.gameObject.AddComponent<BoxCollider>();
                }
            }

            //set previously set values
            //SetBoxColliderVariables();
            boxcollideradd = true;
        }
        else if(addBoxCollider==false && boxcollideradd==true)
        {
            //Debug.Log ("RemoveBoxCollider");

            foreach (Transform child in transform.Find("_Alphabets"))
            {
                DestroyImmediate(child.gameObject.GetComponent<BoxCollider>());
            }

            foreach (Transform child in transform)
            {
                if (child.name != "_Alphabets")
                {
                    DestroyImmediate(child.gameObject.GetComponent<BoxCollider>());
                }
            }
            boxcollideradd = false;
        }

        if (addRigibody==true && rigibodyadd==false)
          {
                foreach (Transform child in transform.Find("_Alphabets"))
                {
                    child.gameObject.AddComponent<Rigidbody>();
                }

                foreach (Transform child in transform)
                {
                    if (child.name != "_Alphabets")
                    {
                        child.gameObject.AddComponent<Rigidbody>();
                    }
                }

            //apply previously set values
            SetRigidbodyVariables();
            rigibodyadd = true;
        }
        else if(addRigibody==false && rigibodyadd==true)
        {
            foreach (Transform child in transform.Find("_Alphabets"))
            {
                DestroyImmediate(child.gameObject.GetComponent<Rigidbody>());
            }

            foreach (Transform child in transform)
            {
                if (child.name != "_Alphabets")
                {
                    DestroyImmediate(child.gameObject.GetComponent<Rigidbody>());
                }
            }
            rigibodyadd = false;
        }
    }

    void Reset()
    {
        GenerateText();
    }

    //Generate New 3D Text
    public void GenerateText()
    {

        //Debug.Log ("GenerateText Called");

        ResetText(); //reset before generating new text

        //check all letters
        for (int ctr = 0; ctr <= text3DtoShow.Length - 1; ctr++)
        {

            Debug.Log("Text Length" + text3DtoShow.Length);
            //Debug.Log ("ctr"+ctr);

            //dealing with linebreaks "\n"
            if (text3DtoShow[ctr].ToString().ToCharArray()[0] == "\n"[0])
            {
                //Debug.Log ("\\n detected");
                _CharXLocation = 0;
                _CharYLocation -= _LineSpacing;
                continue;
            }

            string childObjectName = text3DtoShow[ctr].ToString();


            if (childObjectName != " ")
            {

                GameObject LetterToShow;

                if (childObjectName == "/")
                {
                    LetterToShow = transform.Find("_Alphabets/" + "slash").gameObject; //special case for "/" since it cannot be used for obj name in fbx					
                }
                else if (childObjectName == ".")
                {
                    LetterToShow = transform.Find("_Alphabets/" + "period").gameObject; //special case for "." - naming issue	
                }
                else
                {
                    LetterToShow = transform.Find("_Alphabets/" + childObjectName).gameObject;
                }

                //Debug.Log(LetterToShow);

                AddLetter(LetterToShow);

                //find the width of the letter used
                Mesh mesh = LetterToShow.GetComponent<MeshFilter>().sharedMesh;
                Bounds bounds = mesh.bounds;
                _CharXLocation += bounds.size.x;
                //Debug.Log (bounds.size.x*ObjScale.x);
            }
            else
            {
                _CharXLocation += _SpaceWidth;
            }
        }

#if !UNITY_3_4 && !UNITY_3_5
        //disable child objects inside _Alphabets
        transform.Find("_Alphabets").gameObject.SetActive(false);
#else
		//disable child objects inside _Alphabets
		transform.Find("_Alphabets").gameObject.SetActiveRecursively(false);
#endif

    }


    void AddLetter(GameObject LetterObject)
    {
        Debug.Log("Add New Letter");
        GameObject NewLetter = Instantiate(LetterObject, transform.position, transform.rotation) as GameObject;
        NewLetter.transform.parent = transform; //setting parent relationship

        //rename instantiated object
        NewLetter.name = LetterObject.name;

        //scale accoring to parent obj scale
        float newScaleX = NewLetter.transform.localScale.x * _ObjScale.x;
        float newScaleY = NewLetter.transform.localScale.y * _ObjScale.y;
        float newScaleZ = NewLetter.transform.localScale.z * _ObjScale.z;

        Vector3 newScaleAll = new Vector3(newScaleX, newScaleY, newScaleZ);
        NewLetter.transform.localScale = newScaleAll;
        //------------------------------------

        //dealing with characters with a line down on the left (kerning, especially for use with multiple lines)
        if (_CharXLocation == 0)
            if (NewLetter.name == "B" ||
                NewLetter.name == "D" ||
                NewLetter.name == "E" ||
                NewLetter.name == "F" ||
                NewLetter.name == "H" ||
                NewLetter.name == "I" ||
                NewLetter.name == "K" ||
                NewLetter.name == "L" ||
                NewLetter.name == "M" ||
                NewLetter.name == "N" ||
                NewLetter.name == "P" ||
                NewLetter.name == "R" ||
                NewLetter.name == "U" ||
                NewLetter.name == "b" ||
                NewLetter.name == "h" ||
                NewLetter.name == "i" ||
                NewLetter.name == "k" ||
                NewLetter.name == "l" ||
                NewLetter.name == "m" ||
                NewLetter.name == "n" ||
                NewLetter.name == "p" ||
                NewLetter.name == "r" ||
                NewLetter.name == "u" ||
                NewLetter.name == "|" ||
                NewLetter.name == "[" ||
                NewLetter.name == "!")
                _CharXLocation += 2;

        //position the new char
        NewLetter.transform.localPosition = new Vector3(_CharXLocation, _CharYLocation, 0);

        _CharXLocation += _CharacterSpacing; //add a small space between words
    }


    void ResetText()
    {

        //reset scale
        //transform.localScale = new Vector3(1,1,1);

        //get object scale
        _ObjScale = transform.localScale;

        //reset position
        _CharXLocation = 0f;
        _CharYLocation = 0f;

        //remove all previous created letters
        Transform[] previousLetters;
        previousLetters = GetComponentsInChildren<Transform>();
        foreach (Transform childTransform in previousLetters)
        {
            if (childTransform.name != "_Alphabets" && childTransform.name != transform.name && childTransform.parent.name != "_Alphabets")
            {
                //Debug.Log("previous letter: "+childTransform.name);
                DestroyImmediate(childTransform.gameObject);
            }

        }

    }

    private IEnumerator CreateWatsonService()
    {
        //  Create credential and instantiate service
        Credentials credentials = null;
        if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
        {
            //  Authenticate using username and password
            credentials = new Credentials(_username, _password, _serviceUrl);
        }
        else if (!string.IsNullOrEmpty(_iamApikey))
        {
            //  Authenticate using iamApikey
            TokenOptions tokenOptions = new TokenOptions()
            {
                IamApiKey = _iamApikey,
                IamUrl = _iamUrl
            };

            credentials = new Credentials(tokenOptions, _serviceUrl);

            //  Wait for tokendata
            while (!credentials.HasIamTokenData())
                yield return null;
        }
        else
        {
            throw new WatsonException("Please provide either username and password or IAM apikey to authenticate the service.");
        }

        _Watsonservice = new SpeechToText(credentials);
        _Watsonservice.StreamMultipart = true;

        Active = true;
        StartRecording();
    }

    public bool Active
    {
        get { return _Watsonservice.IsListening; }
        set
        {
            if (value && !_Watsonservice.IsListening)
            {
                _Watsonservice.DetectSilence = true;
                _Watsonservice.EnableWordConfidence = true;
                _Watsonservice.EnableTimestamps = true;
                _Watsonservice.SilenceThreshold = 0.01f;
                _Watsonservice.MaxAlternatives = 0;
                _Watsonservice.EnableInterimResults = true;
                _Watsonservice.OnError = OnError;
                _Watsonservice.InactivityTimeout = -1;
                _Watsonservice.ProfanityFilter = false;
                _Watsonservice.SmartFormatting = true;
                _Watsonservice.SpeakerLabels = false;
                _Watsonservice.WordAlternativesThreshold = null;
                _Watsonservice.StartListening(OnRecognize, OnRecognizeSpeaker);
            }
            else if (!value && _Watsonservice.IsListening)
            {
                _Watsonservice.StopListening();
            }
        }
    }

    private void StartRecording()
    {
        if (_recordingRoutine == 0)
        {
            UnityObjectUtil.StartDestroyQueue();
            _recordingRoutine = Runnable.Run(RecordingHandler());
        }
    }

    private void StopRecording()
    {
        if (_recordingRoutine != 0)
        {
            Microphone.End(_microphoneID);
            Runnable.Stop(_recordingRoutine);
            _recordingRoutine = 0;
        }
    }


    private void OnError(string error)
    {
        Active = false;

        Log.Debug("ExampleStreaming.OnError()", "Error! {0}", error);
    }

    private IEnumerator RecordingHandler()
    {
        Log.Debug("ExampleStreaming.RecordingHandler()", "devices: {0}", Microphone.devices);
        _recording = Microphone.Start(_microphoneID, true, _recordingBufferSize, _recordingHZ);
        yield return null;      // let _recordingRoutine get set..

        if (_recording == null)
        {
            StopRecording();
            yield break;
        }

        bool bFirstBlock = true;
        int midPoint = _recording.samples / 2;
        float[] samples = null;

        while (_recordingRoutine != 0 && _recording != null)
        {
            int writePos = Microphone.GetPosition(_microphoneID);
            if (writePos > _recording.samples || !Microphone.IsRecording(_microphoneID))
            {
                Log.Error("ExampleStreaming.RecordingHandler()", "Microphone disconnected.");

                StopRecording();
                yield break;
            }

            if ((bFirstBlock && writePos >= midPoint)
              || (!bFirstBlock && writePos < midPoint))
            {
                // front block is recorded, make a RecordClip and pass it onto our callback.
                samples = new float[midPoint];
                _recording.GetData(samples, bFirstBlock ? 0 : midPoint);

                AudioData record = new AudioData();
                record.MaxLevel = Mathf.Max(Mathf.Abs(Mathf.Min(samples)), Mathf.Max(samples));
                record.Clip = AudioClip.Create("Recording", midPoint, _recording.channels, _recordingHZ, false);
                record.Clip.SetData(samples, 0);

                _Watsonservice.OnListen(record);

                bFirstBlock = !bFirstBlock;
            }
            else
            {
                // calculate the number of samples remaining until we ready for a block of audio, 
                // and wait that amount of time it will take to record.
                int remaining = bFirstBlock ? (midPoint - writePos) : (_recording.samples - writePos);
                float timeRemaining = (float)remaining / (float)_recordingHZ;

                yield return new WaitForSeconds(timeRemaining);
            }

        }

        yield break;
    }

    private void OnRecognize(SpeechRecognitionEvent result, Dictionary<string, object> customData)
    {
        if (result != null && result.results.Length > 0)
        {
            foreach (var res in result.results)
            {
                foreach (var alt in res.alternatives)
                {
                    string text = string.Format("{0}", alt.transcript);
                    Log.Debug("ExampleStreaming.OnRecognize()", text);
                    _TextToShow = text;
                }

                if (res.keywords_result != null && res.keywords_result.keyword != null)
                {
                    foreach (var keyword in res.keywords_result.keyword)
                    {
                        Log.Debug("ExampleStreaming.OnRecognize()", "keyword: {0}, confidence: {1}, start time: {2}, end time: {3}", keyword.normalized_text, keyword.confidence, keyword.start_time, keyword.end_time);
                    }
                }

                if (res.word_alternatives != null)
                {
                    foreach (var wordAlternative in res.word_alternatives)
                    {
                        Log.Debug("ExampleStreaming.OnRecognize()", "Word alternatives found. Start time: {0} | EndTime: {1}", wordAlternative.start_time, wordAlternative.end_time);
                        foreach (var alternative in wordAlternative.alternatives)
                            Log.Debug("ExampleStreaming.OnRecognize()", "\t word: {0} | confidence: {1}", alternative.word, alternative.confidence);
                    }
                }
            }
        }
    }

    private void OnRecognizeSpeaker(SpeakerRecognitionEvent result, Dictionary<string, object> customData)
    {
        if (result != null)
        {
            foreach (SpeakerLabelsResult labelResult in result.speaker_labels)
            {
                Log.Debug("ExampleStreaming.OnRecognize()", string.Format("speaker result: {0} | confidence: {3} | from: {1} | to: {2}", labelResult.speaker, labelResult.from, labelResult.to, labelResult.confidence));
            }
        }
    }

    public void SetBoxColliderVariables()
    {

        foreach (Transform child in transform.Find("_Alphabets"))
        {
            BoxCollider thisCollider = child.gameObject.GetComponent<BoxCollider>();
            if (thisCollider != null)
            {
                thisCollider.isTrigger = BoxColliderIsTrigger;
            }
        }

        foreach (Transform child in transform)
        {
            BoxCollider thisCollider = child.gameObject.GetComponent<BoxCollider>();
            if (child.name != "_Alphabets" && thisCollider != null)
            {
                thisCollider.isTrigger = BoxColliderIsTrigger;
            }
        }

    }
    public void SetRigidbodyVariables()
    {

        foreach (Transform child in transform.Find("_Alphabets"))
        {
            Rigidbody thisRigidbody = child.gameObject.GetComponent<Rigidbody>();
            if (thisRigidbody != null)
            {
                thisRigidbody.mass = Mass;
                thisRigidbody.drag = Drag;
                thisRigidbody.angularDrag = AngularDrag;
                thisRigidbody.useGravity = UseGravity;
                thisRigidbody.isKinematic = IsKinematic;
                thisRigidbody.interpolation = Interpolation;
                thisRigidbody.collisionDetectionMode = CollisionDetection;

                if (FreezePositionX)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionX;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionX;
                if (FreezePositionY)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionY;
                if (FreezePositionZ)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionZ;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionZ;
                if (FreezeRotationX)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationX;
                if (FreezeRotationY)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezeRotationY;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationY;
                if (FreezeRotationZ)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezeRotationZ;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationZ;

            }
        }

        foreach (Transform child in transform)
        {
            Rigidbody thisRigidbody = child.gameObject.GetComponent<Rigidbody>();
            if (child.name != "_Alphabets" && thisRigidbody != null)
            {
                thisRigidbody.mass = Mass;
                thisRigidbody.drag = Drag;
                thisRigidbody.angularDrag = AngularDrag;
                thisRigidbody.useGravity = UseGravity;
                thisRigidbody.isKinematic = IsKinematic;
                thisRigidbody.interpolation = Interpolation;
                thisRigidbody.collisionDetectionMode = CollisionDetection;

                if (FreezePositionX)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionX;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionX;
                if (FreezePositionY)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionY;
                if (FreezePositionZ)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezePositionZ;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionZ;
                if (FreezeRotationX)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationX;
                if (FreezeRotationY)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezeRotationY;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationY;
                if (FreezeRotationZ)
                    thisRigidbody.constraints |= RigidbodyConstraints.FreezeRotationZ;
                else
                    thisRigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationZ;

            }
        }

    }
}
