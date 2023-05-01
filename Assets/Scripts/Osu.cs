using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Osu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        string filepath = Application.dataPath + "/Songs/1453937 DJ SPIZDIL - Malo Tebya/DJ SPIZDIL - Malo Tebya (SerniGrief) [Extreme].osu";
        Debug.Log(filepath);
        string fileContents = File.ReadAllText(filepath);
        Debug.Log(fileContents);
        osu.Beatmap beatmap = new osu.Beatmap(fileContents);
        Debug.Log(beatmap.ToString());
    }

    // Update is called once per frame
    void Update()
    {

    }
}

namespace osu
{
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

        public string toString()
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
                    hitObjects.Add(new HitObject(x, y, hitType, time, noteLength));
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
}
