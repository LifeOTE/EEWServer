using System;
using System.Collections.Generic;

namespace EEWServer
{
    public class Response//返す緊急地震速報の情報
    {
        public class Item
        {
            public string? latitude { get; set; }
            public string? longitude { get; set; }
            public DateTime reportTime { get; set; }
            public string? regionCode { get; set; }
            public string? regionName { get; set; }
            public string? isCancel { get; set; }
            public string? depth { get; set; }
            public string? calcintensity { get; set; }
            public string? isFinal { get; set; }
            public DateTime originTime { get; set; }
            public string? magnitude { get; set; }
            public string? reportNum { get; set; }
            public string? reportId { get; set; }
            public string? isWarning { get; set; }
        }

        public class HypoInfo
        {
            public List<Item>? items { get; set; }
        }


    }


    public class Yahoo //yahoo!のクラス
    {
        public class Rootobject
        {
            public Hypoinfo hypoInfo { get; set; }
            public object estShindo { get; set; }
            public string[] copyright { get; set; }
            public string version { get; set; }
        }

        public class Item
        {
            public string latitude { get; set; }
            public string longitude { get; set; }
            public string pRadius { get; set; }
            public string sRadius { get; set; }
        }

        public class Hypoinfo
        {
            public Item1[] items { get; set; }
        }

        public class Item1
        {
            public DateTime reportTime { get; set; }
            public string regionCode { get; set; }
            public string regionName { get; set; }
            public string longitude { get; set; }
            public string isCancel { get; set; }
            public string depth { get; set; }
            public string calcintensity { get; set; }
            public string isFinal { get; set; }
            public string isTraining { get; set; }
            public string latitude { get; set; }
            public DateTime originTime { get; set; }
            public string magnitude { get; set; }
            public string reportNum { get; set; }
            public string reportId { get; set; }
        }


    }

    public class iedred
    {
        public class Title
        {
            public int Code { get; set; }
            public string String { get; set; }
            public string Detail { get; set; }
        }

        public class Source
        {
            public int Code { get; set; }
            public string String { get; set; }
        }

        public class Status
        {
            public string Code { get; set; }
            public string String { get; set; }
            public string Detail { get; set; }
        }

        public class AnnouncedTime
        {
            public string String { get; set; }
            public int UnixTime { get; set; }
            public string RFC1123 { get; set; }
        }

        public class OriginTime
        {
            public string String { get; set; }
            public int UnixTime { get; set; }
            public string RFC1123 { get; set; }
        }

        public class Type
        {
            public int Code { get; set; }
            public string String { get; set; }
            public string Detail { get; set; }
        }

        public class Depth
        {
            public int Int { get; set; }
            public string String { get; set; }
            public int Code { get; set; }
        }

        public class Location
        {
            public double Lat { get; set; }
            public double Long { get; set; }
            public Depth Depth { get; set; }
        }

        public class Magnitude
        {
            public double Float { get; set; }
            public string String { get; set; }
            public string LongString { get; set; }
            public int Code { get; set; }
        }

        public class Epicenter
        {
            public int Code { get; set; }
            public string String { get; set; }
            public int Rank2 { get; set; }
            public string String2 { get; set; }
        }

        public class Accuracy
        {
            public Epicenter Epicenter { get; set; }
            public Depth Depth { get; set; }
            public Magnitude Magnitude { get; set; }
            public int NumberOfMagnitudeCalculation { get; set; }
        }

        public class Hypocenter
        {
            public int Code { get; set; }
            public string Name { get; set; }
            public bool isAssumption { get; set; }
            public Location Location { get; set; }
            public Magnitude Magnitude { get; set; }
            public Accuracy Accuracy { get; set; }
            public bool isSea { get; set; }
        }

        public class MaxIntensity
        {
            public string From { get; set; }
            public string To { get; set; }
            public string String { get; set; }
            public string LongString { get; set; }
        }

        public class Reason
        {
            public int Code { get; set; }
            public string String { get; set; }
        }

        public class Change
        {
            public int Code { get; set; }
            public string String { get; set; }
            public Reason Reason { get; set; }
        }

        public class Option
        {
            public Change Change { get; set; }
        }

        public class Root
        {
            public string ParseStatus { get; set; }
            public Title Title { get; set; }
            public Source Source { get; set; }
            public Status Status { get; set; }
            public AnnouncedTime AnnouncedTime { get; set; }
            public OriginTime OriginTime { get; set; }
            public string EventID { get; set; }
            public Type Type { get; set; }
            public int Serial { get; set; }
            public Hypocenter Hypocenter { get; set; }
            public MaxIntensity MaxIntensity { get; set; }
            public bool Warn { get; set; }
            public Option Option { get; set; }
            public string OriginalText { get; set; }
        }

    }
}
