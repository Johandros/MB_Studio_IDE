﻿using importantLib;
using MB_Studio_Library.Objects.Support;
using System;
using System.IO;
using System.Globalization;
using System.Windows.Forms;
using System.Collections.Generic;

namespace MB_Studio_Library.Objects
{
    public class Item : Skriptum
    {
        #region Consts And HeaderVariables

        public const string ZERO_15_CHARS = "000000000000000";

        public static List<HeaderVariable> HeaderIModBits { get; private set; } = new List<HeaderVariable>();
        public static List<HeaderVariable> HeaderIMods { get; private set; } = new List<HeaderVariable>();
        public static List<HeaderVariable> HeaderItemProperties { get; private set; } = new List<HeaderVariable>();
        public static List<HeaderVariable> HeaderItemCapabilityFlags { get; private set; } = new List<HeaderVariable>();

        #endregion

        #region Properties

        #region General

        public static string[] ItemStatsNames = {"weigth", "abundance", "head_armor", "body_armor", "leg_armor", "difficulty", "hit_points",
                                                 "spd_rtng", "shoot_speed", "weapon_length", "max_ammo", "thrust_damage", "swing_damage"};

        public string Name { get; private set; } = string.Empty;
        public string PluralName { get; private set; } = string.Empty;

        public int Price { get; private set; } = 0;
        public double Weight { get; private set; } = 0d;

        public string[] SpecialValues { get; set; } = new string[3];
        public List<string> Triggers { get; set; } = new List<string>();
        public List<int> Factions { get; set; } = new List<int>();
        public List<Variable> Meshes { get; set; } = new List<Variable>();

        #endregion

        #region ItemStats

        public int[] ItemStats { get; set; } = new int[12];

        public int Abundance
        {
            set { ItemStats[0] = value; }
            get { return ItemStats[0]; }
        }

        public int HeadArmor
        {
            set { ItemStats[1] = value; }
            get { return ItemStats[1]; }
        }

        public int BodyArmor
        {
            set { ItemStats[2] = value; }
            get { return ItemStats[2]; }
        }

        public int LegArmor
        {
            set { ItemStats[3] = value; }
            get { return ItemStats[3]; }
        }

        public int Difficulty
        {
            set { ItemStats[4] = value; }
            get { return ItemStats[4]; }
        }

        public int HitPoints
        {
            set { ItemStats[5] = value; }
            get { return ItemStats[5]; }
        }

        public int SpeedRating
        {
            set { ItemStats[6] = value; }
            get { return ItemStats[6]; }
        }

        public int MissileSpeed
        {
            set { ItemStats[7] = value; }
            get { return ItemStats[7]; }
        }

        public int WeaponLength
        {
            set { ItemStats[8] = value; }
            get { return ItemStats[8]; }
        }

        public int MaxAmmo
        {
            set { ItemStats[9] = value; }
            get { return ItemStats[9]; }
        }

        public int ThrustDamage
        {
            set { ItemStats[10] = value; }
            get { return ItemStats[10]; }
        }

        public int SwingDamage
        {
            set { ItemStats[11] = value; }
            get { return ItemStats[11]; }
        }

        #endregion

        #region SpecialValues

        public ulong PropertiesGZ { get { return ulong.Parse(SpecialValues[0]); } }

        public ulong CapabilityFlagsGZ { get { return ulong.Parse(SpecialValues[1]); } }

        public ulong ModBitsGZ { get { return ulong.Parse(SpecialValues[2]); } }

        public string Properties { get { return GetItemPropertiesFromValue(HexConverter.Dec2Hex_16CHARS(PropertiesGZ)); } }

        public string CapabilityFlags { get { return GetItemCapabilityFlagsFromValue(HexConverter.Dec2Hex_16CHARS(CapabilityFlagsGZ)); } }

        public string ModBits { get { return GetItemModifiers_IMODBITS(HexConverter.Dec2Hex_16CHARS(ModBitsGZ)/*, true*/).TrimStart('|'); } }

        #endregion

        #endregion

        #region Initializing

        public Item(string[] values = null) : base(values[0].TrimStart().Split()[0].Substring(4), ObjectType.Item)
        {
            ResetItem();
            if (values != null)
            {
                SetFirstLine(values[0]);
                SetFactionAndTriggerValues(values);
            }
        }

        private void ResetItem()
        {
            Name = string.Empty;
            PluralName = string.Empty;

            Price = 0;
            Weight = 0d;

            Triggers.Clear();
            Factions.Clear();
            Meshes.Clear();

            for (int i = 0; i < SpecialValues.Length; i++)
                SpecialValues[i] = string.Empty;

            for (int i = 0; i < ItemStats.Length; i++)
                ItemStats[i] = 0;
        }

        #region HeaderVariables

        private static List<HeaderVariable> InitializePropsOrCapFlags(string flagMarker)
        {
            List<HeaderVariable> list = new List<HeaderVariable>();
            List<int> masks = new List<int>();

            using (StreamReader sr = new StreamReader(SkillHunter.FilesPath + "header_items.py"))
            {
                while (!sr.EndOfStream)
                {
                    string s = sr.ReadLine().Split('#')[0];
                    if (s.Split('_')[0].Equals(flagMarker))
                    {
                        string[] sp = s.Replace(" ", string.Empty).Split('=');
                        if (sp[1].Contains("0x"))
                            list.Add(new HeaderVariable(sp[1].Substring(2), sp[0]));
                    }
                }
            }

            for (int i = 0; i < list.Count; i++)
                if (list[i].VariableName.EndsWith("mask"))
                    masks.Add(i);
            masks.Reverse();
            foreach (int i in masks)
                list.RemoveAt(i);

            return list;
        }

        private static void InitializeHeaderItemProperties()
        {
            HeaderItemProperties = InitializePropsOrCapFlags("itp");
        }

        private static void InitializeHeaderItemCapabilityFlags()
        {
            HeaderItemCapabilityFlags = InitializePropsOrCapFlags("itcf");
        }

        private static List<HeaderVariable> InitializeModsOrModBits(string flagMarker, bool convToHex)
        {
            List<HeaderVariable> list = new List<HeaderVariable>();
            using (StreamReader sr = new StreamReader(SkillHunter.FilesPath + "header_item_modifiers.py"))
            {
                while (!sr.EndOfStream)
                {
                    string s = sr.ReadLine().Split('#')[0];
                    if (s.Split('_')[0].Equals(flagMarker))
                    {
                        string[] sp = s.Replace(" ", string.Empty).Split('=');
                        list.Add(new HeaderVariable((convToHex) ? HexConverter.Dec2Hex_16CHARS(sp[1]).ToUpper() : sp[1], sp[0]));
                    }
                }
            }
            return list;
        }

        private static void InitializeHeaderIModBits()
        {
            HeaderIModBits = InitializeModsOrModBits("imodbit", true);
        }

        private static void InitializeHeaderIMods()
        {
            HeaderIMods = InitializeModsOrModBits("imod", false);
        }

        #endregion

        #endregion

        #region Set Methods

        private void SetFirstLine(string line)
        {
            string[] xvalues = line.Split();

            //ID = xvalues[0];
            Name = xvalues[1].Replace('_', ' ');
            PluralName = xvalues[2].Replace('_', ' ');

            int tmp = int.Parse(xvalues[3]);//meshCount
            for (int i = 0; i < tmp; i++)
                Meshes.Add(new Variable(xvalues[4 + (i * 2)], ulong.Parse(xvalues[5 + (i * 2)])));

            tmp *= 2;

            SpecialValues[0] = xvalues[tmp + 4];
            SpecialValues[1] = xvalues[tmp + 5];
            Price = int.Parse(xvalues[tmp + 6]);
            SpecialValues[2] = xvalues[tmp + 7];
            Weight = double.Parse(xvalues[tmp + 8], CultureInfo.InvariantCulture);

            for (int i = 0; i < ItemStats.Length; i++)
                ItemStats[i] = int.Parse(xvalues[i + tmp + 9]);
        }

        private void SetFactionAndTriggerValues(string[] values)
        {
            string[] tmpS;
            string tmp = values[1].Trim();
            int x = int.Parse(tmp);

            if (values.Length == 5)
                if (values[2].Equals(string.Empty))
                    values = new string[] { values[0], values[1], values[3], values[4] };

            try
            {
                if (x > 0)
                {
                    tmpS = values[2].Split();
                    for (int i = 0; i < tmpS.Length; i++)
                        Factions.Add(int.Parse(tmpS[i]));
                    x = int.Parse(values[3].Trim());
                    if (HasTriggers(x))
                    {
                        int ix = 4;
                        int endI = x + ix;
                        for (int i = ix; i < endI; i++)
                            Triggers.Add(values[i]);
                    }
                }
                else
                {
                    int ix = 3;
                    int x2 = int.Parse(values[2].Trim());
                    int endI = x2 + ix;
                    if (x == 0 && HasTriggers(x2))
                        for (int i = ix; i < endI; i++)
                            Triggers.Add(values[i]);
                    else if (HasTriggers(x2))
                        ErrorMsg(x, 1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString() + Environment.NewLine + values[0] + ";" + values[2] + ";" + values[3] + ";" + tmp + Environment.NewLine + values.Length);
            }
        }

        #endregion

        #region HeaderVariable Methods

        public static string GetItemPropertiesFromValue(string value)
        {
            string tmp = string.Empty, retur = string.Empty;

            if (HeaderItemProperties.Count == 0)
                InitializeHeaderItemProperties();

            for (int i = 0; i < HeaderItemProperties.Count; i++)
            {
                bool bbbb = false;
                tmp = HeaderItemProperties[i].VariableValue.TrimStart('0');
                int curIdx = value.Length - tmp.Length;
                if (value.Length >= tmp.Length)
                {
                    if (tmp[0] == value[curIdx] && tmp.Length > 2)
                        retur += "|" + HeaderItemProperties[i].VariableName;
                    else
                        bbbb = true;

                    if (value[curIdx] != '0' && bbbb)
                    {
                        List<HeaderVariable> list = new List<HeaderVariable>();
                        uint x_counter = 0;
                        uint x_tmp = uint.Parse(HexConverter.Hex2Dec(value.Substring(curIdx, 1)).ToString());

                        if (tmp.Length > 2)
                        {
                            for (int j = 9; j < HeaderItemProperties.Count; j++)
                                if (HeaderItemProperties[j].VariableValue.TrimStart('0').Length == value.Substring(curIdx).Length)
                                    list.Add(HeaderItemProperties[j]);
                            list.Reverse();
                            foreach (HeaderVariable variable in list)
                            {
                                if (x_counter < x_tmp)
                                {
                                    uint x_tmp2 = uint.Parse(HexConverter.Hex2Dec(variable.VariableValue.Trim('0')).ToString());
                                    uint tttt = x_tmp2 + x_counter;
                                    if (x_tmp2 <= x_tmp && tttt <= x_tmp)
                                    {
                                        x_counter += x_tmp2;
                                        if (!retur.Contains(variable.VariableName))
                                            retur += "|" + variable.VariableName;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (value.Length > 2)
                tmp = value.Substring(value.Length - 2);

            tmp = tmp.TrimStart('0');

            foreach (HeaderVariable itype in HeaderItemProperties)
            {
                string tmp2 = itype.VariableValue.TrimStart('0').ToLower();
                if (tmp2.Length < 3)
                    if (tmp.ToLower().Equals(tmp2))
                        retur += "|" + itype.VariableName;
            }

            if (retur.Length != 0)
                retur = retur.Substring(1);

            string[] tmpS = retur.Split('|');
            SkillHunter.RemoveItemDuplicatesFromArray(ref tmpS);
            retur = string.Empty;
            for (int i = 0; i < tmpS.Length; i++)
            {
                retur += tmpS[i];
                if (i < tmpS.Length - 1)
                    retur += '|';
            }

            if (retur.Length == 0)
                retur = "0";

            return retur;
        }

        private static void Handle2SizeCFs(ref string varStart)
        {
            if (varStart.Length == 9)
            {
                if (varStart.TrimEnd('0').Length == 1)
                    varStart += ".";
            }
            else if (varStart.Length == 8)
                varStart = "." + varStart;
        }

        private static void HandleCFs(ref string retur, ref ulong x_tmp, ref ulong x_counter, ref string value, HeaderVariable variable)
        {
            string varStart = variable.VariableValue.TrimStart('0');
            Handle2SizeCFs(ref varStart);

            varStart = varStart.Replace("0", string.Empty);

            if (varStart.Length == 0) return;

            varStart = varStart.Replace('.', '0');

            ulong xtert = HexConverter.Hex2Dec(varStart);
            ulong ttttt = xtert + x_counter;
            if (xtert <= x_tmp && ttttt <= x_tmp)
            {
                bool isValid = (varStart.Length == 1);
                if (!isValid)
                {
                    string specialValue = value.Substring(7, 2).ToUpper();
                    string val2 = varStart.Substring(0, 2).ToUpper();
                    isValid = specialValue.Equals(val2);

                    if (!isValid)
                    {
                        ulong spec = HexConverter.Hex2Dec(specialValue);
                        isValid = (spec > 0x80 && (specialValue[0] == val2[0] || specialValue[1] == val2[1]));
                    }
                }

                if (isValid)
                {
                    x_counter = ttttt;
                    if (!IsValueInValueString(retur, variable.VariableName))
                        retur += "|" + variable.VariableName;
                }
            }
        }

        public static string GetItemCapabilityFlagsFromValue(string value)
        {
            string retur = string.Empty;

            if (HeaderItemCapabilityFlags.Count == 0)
            {
                InitializeHeaderItemCapabilityFlags();
                HeaderItemCapabilityFlags.Reverse();
            }

            int valueLength = 16;
            while (value.Length < valueLength)
                value = "0" + value;

            for (int i = 0; i < valueLength; i++)
            {
                if (value[i] == '0') continue;

                List<HeaderVariable> list = new List<HeaderVariable>();

                int subLength = 1;
                if (i == 7)
                    subLength++;

                int curLength = valueLength - i;

                ulong x_counter = 0;
                ulong x_tmp = HexConverter.Hex2Dec(value.Substring(i, subLength));

                for (int j = 0; j < HeaderItemCapabilityFlags.Count; j++)
                {
                    string varValue = HeaderItemCapabilityFlags[j].VariableValue.TrimStart('0');
                    if (varValue.Length == curLength)
                        list.Add(HeaderItemCapabilityFlags[j]);
                }

                foreach (HeaderVariable variable in list)
                    if (x_counter < x_tmp)
                        HandleCFs(ref retur, ref x_tmp , ref x_counter, ref value, variable);
            }

            if (!retur.Equals(string.Empty))
                retur = retur.Substring(1);

            string[] tmpS = retur.Split('|');
            SkillHunter.RemoveItemDuplicatesFromArray(ref tmpS);
            retur = string.Empty;
            for (int i = 0; i < tmpS.Length; i++)
            {
                retur += tmpS[i];
                if (i < tmpS.Length - 1)
                    retur += "|";
            }

            if (retur.Equals(string.Empty))
                retur = "0";

            return retur;
        }

        private static bool IsValueInValueString(string valueString, string value)
        {
            bool yes = false;
            string[] sp = valueString.Trim().Split('|');
            foreach (string s in sp)
                if (s.Equals(value))
                    yes = true;
            return yes;
        }

        public static string GetItemModifiers_IMODBITS(string value/*, bool imodbits = false*/)
        {
            string retur = string.Empty;
            if (HeaderIModBits.Count == 0)
            {
                InitializeHeaderIModBits();
                HeaderIModBits.Reverse();
            }

            int valueLength = 16;
            while (value.Length < valueLength)
                value = "0" + value;

            for (int i = 0; i < valueLength; i++)
            {
                if (value[i] == '0') continue;

                List<HeaderVariable> list = new List<HeaderVariable>();

                int curLength = valueLength - i;

                ulong x_counter = 0;
                ulong x_tmp = HexConverter.Hex2Dec(value.Substring(i, 1));

                for (int j = 0; j < HeaderIModBits.Count; j++)
                {
                    string varValue = HeaderIModBits[j].VariableValue.TrimStart('0');
                    if (varValue.Length == curLength)
                        list.Add(HeaderIModBits[j]);
                }

                foreach (HeaderVariable variable in list)
                {
                    string varStart = variable.VariableValue.Replace("0", string.Empty);
                    ulong xtert = HexConverter.Hex2Dec(varStart);
                    ulong ttttt = xtert + x_counter;
                    if (xtert <= x_tmp && ttttt <= x_tmp)
                    {
                        x_counter = ttttt;
                        if (!IsValueInValueString(retur, variable.VariableName))
                            retur += "|" + variable.VariableName;
                    }
                }
            }

            if (retur.Length != 0)
                retur = retur.Trim('|');

            string[] tmpS = retur.Split('|');
            SkillHunter.RemoveItemDuplicatesFromArray(ref tmpS);
            retur = string.Empty;
            for (int i = 0; i < tmpS.Length; i++)
            {
                retur += tmpS[i];
                if (i < tmpS.Length - 1)
                    retur += "|";
            }

            if (retur.Equals(string.Empty))
                retur = "imodbits_none";

            return retur;
        }

        public static string GetItemModifiers_IMODS(string value)//USE THIS METHOD FOR COMBINING OF MODBITS !!! 
        {
            string retur = string.Empty;

            if (HeaderIMods.Count == 0)
                InitializeHeaderIMods();

            retur = value;// UNTIL CODE IS WRITTEN JUST USE DEFAULT

            /*for (int i = 0; i < headerIMods.Count; i++)
            {
                //CODE HERE
            }*/

            return retur;
        }

        #endregion

        #region Helper Methods

        public static ulong GetModBitStringToValue(string modbitString)
        {
            ulong value = 0ul;
            string[] tmp = modbitString.Trim(' ', '|').Split('|');
            foreach (HeaderVariable var in HeaderIModBits)
            {
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (var.VariableName.Equals(tmp[i]))
                    {
                        value |= HexConverter.Hex2Dec_16CHARS(var.VariableValue);
                        i = tmp.Length;
                    }
                }
            }
            return value;
        }

        public static string GetMeshKindFromValue(ulong value)
        {
            string retur = string.Empty;

            if (value != 0u)
            {
                string valS = HexConverter.Dec2Hex_16CHARS(value);
                if (valS[0] == '1')
                    retur = "ixmesh_inventory";
                else if (valS[0] == '2')
                    retur = "ixmesh_flying_ammo";
                else if (valS[0] == '3')
                    retur = "ixmesh_carry";

                if (!valS.Substring(1).Equals(ZERO_15_CHARS))
                {
                    retur += "|" + GetItemModifiers_IMODBITS(valS/*, true*/);
                    retur = retur.Trim('|');
                }
            }

            if (retur.Length == 0)
                retur = "0";// "none"

            return retur;
        }

        public static ulong GetValueFromMeshKind(string meshKind)
        {
            ulong retur = 0;

            List<string> meshKinds = new List<string>() { "inventory", "flying_ammo", "carry" };

            string meshModifierBits = string.Empty;
            int modifIdx = meshKind.IndexOf('|') + 1;
            if (modifIdx > 0)
            {
                meshModifierBits = meshKind.Substring(modifIdx);
                meshKind = meshKind.Remove(modifIdx);
            }
            meshKind = meshKind.Replace("ixmesh_", string.Empty);

            int kindIdx = meshKinds.IndexOf(meshKind);
            string hexValue = string.Empty;
            if (kindIdx >= 0)
                hexValue += kindIdx;

            //if ()

            return retur;
        }

        private bool HasTriggers(int x)
        {
            bool b = false;
            if (x > 0)
                b = true;
            else if (x != 0)
                ErrorMsg(x, 2);
            return b;
        }

        private void ErrorMsg(int x, int errorNumber)
        {
            MessageBox.Show("There was an error somewhere in the file! --> x = " + x.ToString() + " : 0x" + errorNumber.ToString());
        }

        #endregion
    }
}
