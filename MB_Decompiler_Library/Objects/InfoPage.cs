﻿using skillhunter;

namespace MB_Decompiler_Library.Objects
{
    public class InfoPage : Skriptum
    {
        private string name, text;

        public InfoPage(string[] raw_data) : base(raw_data[0].Substring(3), ObjectType.INFO_PAGE)//remove/change SubString(3) if better/possible
        {
            name = raw_data[1].Replace('_', ' ');
            text = raw_data[2].Replace('_', ' ');
        }

        public string Text { get { return text; } }

        public string Name { get { return name; } }

    }
}