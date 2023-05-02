using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Osu : MonoBehaviour
{
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private GameObject wallPrefab;
    private AudioSource audioSource;

    private Beatmap beatmap;
    private List<GameObject> platforms = new List<GameObject>();
    private PlayerMovementAdvanced player;

    private float walkSpeed;
    private float timeMod;


    // Start is called before the first frame update
    void Start()
    {
        // Get the player
        player = GameObject.Find("Player").GetComponent<PlayerMovementAdvanced>();

        string filepath = Application.dataPath + "/Songs/185250 ALiCE'S EMOTiON - Dark Flight Dreamer/ALiCE'S EMOTiON - Dark Flight Dreamer (Sakaue Nachi) [Dreamer].osu";
        string fileContents = File.ReadAllText(filepath);
        beatmap = new Beatmap(fileContents);
        Debug.Log(beatmap.ToString());
        walkSpeed = player.walkSpeed;
        timeMod = walkSpeed / 1000.0f;
        GeneratePlatforms();

        audioSource = GetComponent<AudioSource>();
        // // Create an audio clip from the beatmap's audio filepath
        // AudioClip audioClip = Resources.Load<AudioClip>(Application.dataPath + "/Songs/1453937 DJ SPIZDIL - Malo Tebya/" + beatmap.AudioFilename());
        // // Set the audio source's clip to the audio clip
        // audioSource.clip = audioClip;
        // // Set the audio source's volume
        audioSource.volume = 0.25f;
        // // Play the audio source
        // audioSource.Play();


        if (player != null)
        {
            // Set the player's position to the first platform
            player.transform.position = platforms[0].transform.position + new Vector3(0, 10, 0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (audioSource != null)
        {
            // If the audio source is not playing, play it
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }

            // If the audio source is playing, set the player's position to the audio source's time
            if (audioSource.isPlaying)
            {
                if (player != null)
                {
                    // Get the current time
                    float time = audioSource.time;
                    int index = 0;
                    // Find the platform that is closest to the current time
                    for (index = 0; index < platforms.Count - 1 && platforms[index + 1].transform.position.z / walkSpeed <= time; index++) { }

                    float ypos = 0;
                    if (index < platforms.Count - 1 && time > platforms[0].transform.position.z / timeMod / 1000.0f)
                    {
                        GameObject currentPlatform = platforms[index];
                        GameObject nextPlatform = platforms[index + 1];

                        Vector3 startPos = currentPlatform.transform.position;
                        Vector3 endPos = nextPlatform.transform.position;

                        if (nextPlatform.layer == 10)
                        {
                            Vector3 bounds = nextPlatform.GetComponent<MeshFilter>().mesh.bounds.size;
                            endPos.z -= bounds.z / 2;
                        }
                        if (currentPlatform.layer == 10)
                        {
                            Vector3 bounds = currentPlatform.GetComponent<MeshFilter>().mesh.bounds.size;
                            startPos.z += bounds.z / 2;
                        }

                        // Debug.Log($"{player.transform.position.z} ? {currentPlatform.z} - {nextPlatform.z}");
                        float distance = endPos.z - startPos.z;
                        float pz = player.transform.position.z - startPos.z;
                        if (pz < 0 || pz > distance)
                        {
                            ypos = 0;
                        }
                        else
                        {
                            float half = distance / 2;
                            float grav = -0.2f;
                            float halfy = 0.5f * grav * (half * half - distance * half);
                            if (halfy > 10)
                            {
                                grav = 10 / (0.5f * (half * half - distance * half));
                            }
                            ypos = 0.5f * grav * (pz * pz - distance * pz);
                        }

                    }

                    Transform cam = GameObject.Find("PlayerCam").GetComponent<PlayerCam>().orientation;
                    Debug.Log(cam.forward);
                    float xpos = player.transform.position.x + cam.forward.x * walkSpeed * Time.deltaTime;
                    float zpos = time * walkSpeed;
                    player.transform.position = new Vector3(xpos, ypos, zpos);

                }
            }
        }
    }

    void GeneratePlatforms()
    {
        bool wallLeft = false;
        foreach (HitObject hitObject in beatmap.HitObjects())
        {
            // Debug.Log(hitObject.ToString());
            float xpos = Mathf.Sin(hitObject.Time() / 100.0f);
            if (hitObject.Type() == HitObjectType.CIRCLE)
            {
                // Create a platform
                GameObject platform = Instantiate(platformPrefab);
                float length = 10 * timeMod;
                // Get the bounds of the platform
                Bounds bounds = platform.GetComponent<MeshFilter>().mesh.bounds;
                // Adjust the length of the platform to the bounds
                length /= bounds.size.z;
                // Set the platform's scale
                platform.transform.localScale = new Vector3(length, length, 1);
                // Set the platform layer
                platform.layer = 10;
                // Set the position to the time
                platform.transform.position = new Vector3(xpos, -bounds.size.z, hitObject.Time() * timeMod);
                platforms.Add(platform);
            }
            else
            {
                // Create a wall
                GameObject wall = Instantiate(wallPrefab);
                float length = hitObject.Length() * timeMod;
                // Get the bounds of the wall
                Bounds bounds = wall.GetComponent<MeshFilter>().mesh.bounds;
                // Adjust the length of the wall to the bounds
                length /= bounds.size.x;
                // Set the wall's scale
                wall.transform.localScale = new Vector3(length, 1, 1);
                // Set the wall layer
                wall.layer = 9;
                wall.transform.rotation = Quaternion.Euler(0, 90, 0);
                // Set the position to the time
                if (wallLeft)
                {
                    xpos = 2.5f;
                }
                else
                {
                    xpos = -2.5f;
                }
                wall.transform.position = new Vector3(xpos, bounds.size.y / 4, hitObject.Time() * timeMod);
                platforms.Add(wall);
                wallLeft = !wallLeft;
            }
        }
    }
}

enum HitObjectType
{
    CIRCLE = 0,
    SLIDER = 1,
    SPINNER = 3
};

class HitObject
{
    public HitObject(int x, int y, HitObjectType type, int time, int length)
    {
        this.x = x;
        this.y = y;
        this.type = type;
        this.time = time;
        this.length = length;
    }

    int x;
    int y;
    HitObjectType type;
    int time;
    int length;

    public int X()
    {
        return x;
    }

    public int Y()
    {
        return y;
    }

    public HitObjectType Type()
    {
        return type;
    }

    public int Time()
    {
        return time;
    }

    public int Length()
    {
        return length;
    }

    public override string ToString()
    {
        string typeStr = "";
        switch (type)
        {
            case HitObjectType.CIRCLE:
                typeStr = "CIRCLE";
                break;
            case HitObjectType.SLIDER:
                typeStr = "SLIDER";
                break;
            case HitObjectType.SPINNER:
                typeStr = "SPINNER";
                break;
        }

        return string.Format("x: {0}, y: {1}, type: {2}, time: {3}, length: {4}", x, y, typeStr, time, length);
    }
};
class TimingPoint
{
    public TimingPoint(int time, float beatLength, bool uninherited)
    {
        this.time = time;
        this.beatLength = beatLength;
        this.uninherited = uninherited;
    }

    int time;
    float beatLength;
    bool uninherited;

    public int Time()
    {
        return time;
    }

    public float BeatLength()
    {
        return beatLength;
    }

    public bool Uninherited()
    {
        return uninherited;
    }

    public float SliderVelocity()
    {
        if (uninherited)
        {
            return -100.0f / beatLength;
        }
        else
        {
            return 1.0f;
        }
    }
};
class Beatmap
{
    public Beatmap(string osufile)
    {
        leadin = 0;
        hitObjects = new List<HitObject>();
        processOsuFile(osufile);
    }

    private int leadin;
    private List<HitObject> hitObjects;
    private string audioFilename;
    private string title;
    private string artist;
    private int beatmapID;


    public int LeadIn()
    {
        return leadin;
    }

    public int EndTime()
    {
        HitObject last = hitObjects[hitObjects.Count - 1];
        return last.Time() + last.Length();
    }

    public List<HitObject> HitObjects()
    {
        return hitObjects;
    }

    public string AudioFilename()
    {
        return audioFilename;
    }

    public string Title()
    {
        return title;
    }

    public string Artist()
    {
        return artist;
    }

    public int BeatmapID()
    {
        return beatmapID;
    }

    private void processOsuFile(string osufile)
    {
        List<TimingPoint> timingPoints = new List<TimingPoint>();

        float sliderMultiplier = 1.0f;
        string section = "";
        foreach (string rawline in osufile.Split('\n'))
        {
            string line = rawline.Trim();
            if (line.Length < 1)
                continue;

            string[] args = line.Split(',');

            if (line[0] == '[')
            {
                section = line.Substring(1, line.LastIndexOf(']') - 1);
            }
            else if (section == "General")
            {
                if (line.LastIndexOf("AudioFilename") == 0)
                {
                    audioFilename = line.Substring(line.IndexOf(':') + 1);
                }
                else if (line.LastIndexOf("AudioLeadIn") == 0)
                {
                    leadin = int.Parse(line.Substring(line.IndexOf(':') + 1));
                }
            }
            else if (section == "Metadata")
            {
                if (line.LastIndexOf("Title") == 0)
                {
                    title = line.Substring(line.IndexOf(':') + 1);
                }
                else if (line.LastIndexOf("Artist") == 0)
                {
                    artist = line.Substring(line.IndexOf(':') + 1);
                }
                else if (line.LastIndexOf("BeatmapID") == 0)
                {
                    beatmapID = int.Parse(line.Substring(line.IndexOf(':') + 1));
                }
            }
            else if (section == "Difficulty")
            {
                if (line.LastIndexOf("SliderMultiplier") == 0)
                {
                    sliderMultiplier = float.Parse(line.Substring(line.IndexOf(':') + 1));
                }
            }
            else if (section == "TimingPoints")
            {
                int time = int.Parse(args[0]) + leadin;
                float beatLength = float.Parse(args[1]);
                int uninherited = int.Parse(args[6]);

                timingPoints.Add(new TimingPoint(time, beatLength, uninherited == 0));
            }
            else if (section == "HitObjects")
            {
                int x = int.Parse(args[0]);
                int y = int.Parse(args[1]);

                int time = int.Parse(args[2]) + leadin;
                int noteLength = 0;

                float sliderVelocity = 1.0f;
                float beatLength = timingPoints[0].BeatLength();

                for (int i = 0; i < timingPoints.Count; i++)
                {
                    if (timingPoints[i].Time() <= time)
                    {
                        if (!timingPoints[i].Uninherited())
                        {
                            sliderVelocity = timingPoints[i].SliderVelocity();
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                int type = int.Parse(args[3]);
                HitObjectType hitType = HitObjectType.CIRCLE;
                if ((type & 0b00000010) != 0)
                {
                    hitType = HitObjectType.SLIDER;
                    int length = (int)float.Parse(args[7]);
                    int slides = (int)float.Parse(args[6]);
                    noteLength = (int)(length / (sliderMultiplier * 100 * sliderVelocity) * beatLength * slides);
                }
                else if ((type & 0b00001000) != 0)
                {
                    hitType = HitObjectType.SPINNER;
                    noteLength = int.Parse(args[5]) - time;
                }
                HitObject obj = new HitObject(x, y, hitType, time, noteLength);
                // Debug.Log(obj.ToString());
                hitObjects.Add(obj);
            }
        }
    }

    override public string ToString()
    {
        string str = string.Empty;
        str += "Title: " + Title() + "\n";
        str += "Artist: " + Artist() + "\n";
        str += "AudioFilename: " + AudioFilename() + "\n";
        str += "BeatmapID: " + BeatmapID() + "\n";
        str += "LeadIn: " + LeadIn() + "ms" + "\n";

        int numCircles = 0;
        int numSliders = 0;
        int numSpinners = 0;
        foreach (HitObject obj in HitObjects())
        {
            switch (obj.Type())
            {
                case HitObjectType.CIRCLE:
                    numCircles++;
                    break;
                case HitObjectType.SLIDER:
                    numSliders++;
                    break;
                case HitObjectType.SPINNER:
                    numSpinners++;
                    break;
            }
        }
        str += HitObjects().Count + " HitObjects (" + numCircles + " circles, " + numSliders + " sliders, " + numSpinners + " spinners)" + "\n";

        return str;
    }
}

