﻿using MB_Decompiler;
using MB_Decompiler_Library.IO;
using skillhunter;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WarbandTranslator;
using brfManager;
using importantLib;
using MB_Studio.Main;

namespace MB_Studio.Manager
{
    public partial class TroopManager : SpecialForm
    {
        #region Attributes

        private FileSaver fileSaver;

        private Thread openBrfThread;
        private OpenBrfManager openBrfManager = null;

        private const string FACE_CODE_ZERO = "0x0000000000000000000000000000000000000000000000000000000000000000";

        private List<Troop> troops = new List<Troop>();
        private List<string> items = new List<string>();
        private Item[] itemsRList = new Item[] { };

        private List<ulong> inventoryItemFlags = new List<ulong>();
        private List<string[]> translations = new List<string[]>();

        // instance member to keep reference to splash form
        private SplashForm frmSplash;
        // delegate for the UI updater
        public delegate void UpdateUIDelegate(bool IsDataLoaded);

        #endregion

        #region Loading

        public TroopManager()
        {
            InitializeComponent();
            title_lbl.MouseDown += Control_MoveForm_MouseDown;
        }

        private void TroopManager_Load(object sender, EventArgs e)
        {
            title_lbl.Text = Text;
            // Update UI
            UpdateUI(false);

            // Show the splash form
            frmSplash = new Loader(this, false)
            {
                StartPosition = FormStartPosition.CenterScreen
            };
            frmSplash.Show();

            // Do some time consuming work in separate thread
            Thread t = new Thread(new ThreadStart(LoadControlsAndSettings)) { IsBackground = true };
            t.Start();
        }

        private void LoadSettingsAndLists()
        {
            InitializeLists();
            LoadSets();
            CreateSkillGroupBox();
        }

        /// <summary>
        /// Updates the UI.
        /// </summary>
        protected void UpdateUI(bool IsDataLoaded)
        {
            if (IsDataLoaded)
            {
                //DONE INFO
                Opacity = 100;
                // close the splash form
                if (frmSplash != null)
                    frmSplash.Close();
            }
        }

        private void LoadControlsAndSettings()
        {
            LoadSettingsAndLists();

            Invoke((MethodInvoker)delegate
            {
                openBrfThread = new Thread(StartOpenBrfManager) { IsBackground = true };
                openBrfThread.Start();
            });

            // Update UI
            //Invoke(new UpdateUIDelegate(UpdateUI), new object[] { true }); // IS IN openBrfThread!
        }

        private void InitializeLists()
        {
            CodeReader cr = new CodeReader(CodeReader.ModPath + CodeReader.Files[(int)Skriptum.ObjectType.TROOP]);
            Troop[] troopsX = cr.ReadTroop();

            bool loadSavedTroops = true; // maybe make this universal in MB Studio as option to select but default true

            List<Troop> savedTroops = new List<Troop>();
            if (loadSavedTroops)
            {
                List<List<string>> savedTroopsDatas = MB_Studio.LoadAllPseudoCodeByObjectTypeID((int)Skriptum.ObjectType.TROOP);
                foreach (List<string> savedTroopData in savedTroopsDatas)
                    savedTroops.Add(new Troop(CodeReader.GetStringArrayStartFromIndex(savedTroopData.ToArray(), 1)));
            }

            cr = new CodeReader(CodeReader.ModPath + CodeReader.Files[(int)Skriptum.ObjectType.ITEM]);
            itemsRList = cr.ReadItem();

            bool found = false;
            foreach (Troop troop in troopsX)
            {
                if (loadSavedTroops)
                {
                    found = false;
                    for (int i = 0; i < savedTroops.Count; i++)
                    {
                        if (troop.ID.Equals(savedTroops[i].ID))
                        {
                            troops.Add(savedTroops[i]);
                            found = true;
                            i = savedTroops.Count;
                        }
                    }
                }
                if (!found)
                    troops.Add(troop);
            }

            Invoke((MethodInvoker)delegate
            {
                foreach (Control c in troopPanel.Controls)
                    if (c.Name.Split('_')[0].Equals("showGroup"))
                        c.Click += C_Click;
                for (int i = 0; i < CodeReader.Items.Length; i++)
                {
                    items.Add(i + " - " + CodeReader.Items[i]);
                    items_lb.Items.Add(items[items.Count - 1]);
                }
                foreach (string scene in CodeReader.Scenes)
                    scenes_lb.Items.Add(scene);
                foreach (string faction in CodeReader.Factions)
                    factions_lb.Items.Add(faction);
                foreach (Troop troop in troops)
                {
                    troopSelect_lb.Items.Add(troop.ID);
                    upgradeTroop1_lb.Items.Add(troop.ID);
                    upgradeTroop2_lb.Items.Add(troop.ID);
                }
                upgradeTroop1_lb.Items.RemoveAt(1);
                upgradeTroop2_lb.Items.RemoveAt(1);
                if (!Directory.Exists(CodeReader.ModPath + "languages"))
                    Directory.CreateDirectory(CodeReader.ModPath + "languages");
                languages_cbb.SelectedIndex = 0;
                foreach (string item in Directory.GetDirectories(CodeReader.ModPath + "languages"))
                    if (!ComboBoxContainsInLines(languages_cbb, item.Substring(item.LastIndexOf('\\') + 1), '('))
                        languages_cbb.Items.Add(item.Substring(item.LastIndexOf('\\') + 1) + " (" + item.Substring(item.LastIndexOf('\\') + 1).ToUpper() + ')');
                for (int i = 0; i < languages_cbb.Items.Count; i++)
                    translations.Add(new string[2]);
            });
        }

        private void CreateSkillGroupBox()
        {
            Font font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Color backColor = Color.FromArgb(56, 56, 56);
            Color foreColor = Color.White;
            int tabIndex;
            Invoke((MethodInvoker)delegate
            {
                for (int i = 0; i < SkillHunter.Skillnames.Length - 8; i++)
                {
                    tabIndex = (i / 8 + 1) * 8 - (i - (i / 8) * 8) - 1;
                    //MessageBox.Show("Index: " + tabIndex);
                    // 
                    // label
                    // 
                    Label skillLabelX = new Label()
                    {
                        BackColor = backColor,
                        Font = font,
                        ForeColor = foreColor,
                        Location = new Point(6 + (i / 8) * 145, 27 + i * 26 - (i / 8) * 208),
                        Name = SkillHunter.Skillnames[i] + "lbl",
                        TabIndex = tabIndex,
                        Tag = i,
                        TabStop = false,
                        Size = new Size(100, 13),
                        TextAlign = ContentAlignment.MiddleRight
                    };
                    string text = SkillHunter.Skillnames[i].Replace('_', ' ').TrimEnd();
                    int length = Math.Min(text.Length - 1, 12);
                    text = text.Substring(0, 1).ToUpper() + text.Substring(1, length) + ':';
                    skillLabelX.Text = text;
                    groupBox_6_gb.Controls.Add(skillLabelX);

                    // 
                    // numeric
                    // 
                    NumericUpDown skillNumericX = new NumericUpDown()
                    {
                        BackColor = backColor,
                        BorderStyle = BorderStyle.FixedSingle,
                        Font = font,
                        ForeColor = foreColor,
                        Location = new Point(110 + (i / 8) * 145, 25 + i * 26 - (i / 8) * 208),
                        Maximum = 15,
                        Name = SkillHunter.Skillnames[i] + "num",
                        Size = new Size(34, 20),
                        TabIndex = tabIndex,
                        Tag = i,
                        TabStop = false
                    };
                    groupBox_6_gb.Controls.Add(skillNumericX);
                }
            });
        }

        #endregion

        #region GUI

        private void C_Click(object sender, EventArgs e)
        {
            bool sub = false;
            Control button = (Control)sender;
            int index = int.Parse(button.Name.Split('_')[1]);
            Control groupBox = troopPanel.Controls.Find("groupBox_" + index + "_gb", false)[0];
            if (groupBox.Height == ToolForm.GROUP_HEIGHT_MIN)
            {
                groupBox.Height = ToolForm.GROUP_HEIGHT_MAX;
                if (button.Equals(showGroup_6_btn))// || button.Equals(showGroup_3_btn))
                    groupBox.Height += ToolForm.GROUP_HEIGHT_MAX + ToolForm.GROUP_HEIGHT_MIN;
                else if (button.Equals(showGroup_8_btn))
                    groupBox.Height -= ToolForm.GROUP_HEIGHT_MIN;
                else if (button.Equals(showGroup_3_btn))
                    groupBox.Height = 400;
                button.Text = "ʌ";
            }
            else
            {
                groupBox.Height = ToolForm.GROUP_HEIGHT_MIN;
                button.Text = "v";
                sub = !sub;
            }
            button.Height = groupBox.Height;
            int differ = ToolForm.GROUP_HEIGHT_DIF;
            if (button.Equals(showGroup_6_btn))// || button.Equals(showGroup_3_btn))
                differ += ToolForm.GROUP_HEIGHT_MAX + ToolForm.GROUP_HEIGHT_MIN;
            else if (button.Equals(showGroup_8_btn))
                differ = differ - ToolForm.GROUP_HEIGHT_MIN;
            else if (button.Equals(showGroup_3_btn))
                differ = 375;
            foreach (Control c in troopPanel.Controls)
            {
                if (int.Parse(c.Name.Split('_')[1]) > index)
                {
                    if (!sub)
                        c.Top += differ;
                    else
                        c.Top -= differ;
                }
            }
            if (!sub)
                Height += differ;
            else
                Height -= differ;
        }

        private void SearchTroop_SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            troopSelect_lb.Items.Clear();
            if (!searchTroop_SearchTextBox.Text.Contains("Search ...") && searchTroop_SearchTextBox.Text.Length > 0)
            {
                foreach (Troop troop in troops)
                    if (troop.ID.Contains(searchTroop_SearchTextBox.Text) || troop.ID.Contains(searchTroop_SearchTextBox.Text))
                        troopSelect_lb.Items.Add(troop.ID);
            }
            else
                foreach (Troop troop in troops)
                    troopSelect_lb.Items.Add(troop.ID);
        }

        private void Exit_btn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Min_btn_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void TroopSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (troopSelect_lb.Items.Count > 0)
            {
                if (troopSelect_lb.SelectedIndex > 0 || !troopSelect_lb.SelectedItem.ToString().Equals("New"))
                    SetupTroop(troops[GetIndexOfTroopByID(troopSelect_lb.SelectedItem.ToString())]);
                else
                    ResetControls();
            }
        }

        private int GetIndexOfTroopByID(string id)
        {
            int index = -1;
            for (int i = 0; i < troops.Count; i++)
            {
                if (troops[i].ID.Equals(id))
                {
                    index = i;
                    i = troops.Count;
                }
            }
            return index;
        }

        private void ResetControls()
        {
            id_txt.ResetText();
            name_txt.ResetText();
            plural_name_txt.ResetText();
            usedItems_lb.Items.Clear();
            foreach (Control groupC in troopPanel.Controls)
            {
                if (groupC.Name.Substring(groupC.Name.LastIndexOf('_') + 1).Equals("gb"))
                {
                    GroupBox groupBox = (GroupBox)groupC;
                    foreach (Control c in groupBox.Controls)
                    {
                        string nameEnd = GetNameEndOfControl(c);
                        if (nameEnd.Equals("txt"))
                            c.Text = "0";
                        else if (nameEnd.Equals("num") || c.Name.Substring(c.Name.LastIndexOf('_') + 1).Equals("numeric"))
                            ((NumericUpDown)c).Value = 0;
                        else if (nameEnd.Equals("lb"))
                        {
                            ListBox lb = (ListBox)c;
                            if (lb.Items.Count > 0)
                                lb.SelectedIndex = 0;
                        }
                        else if (nameEnd.Equals("cb"))
                            ((CheckBox)c).CheckState = CheckState.Unchecked;
                        else if (nameEnd.Equals("SearchTextBox"))
                            c.Text = "Search ...";
                    }
                }
            }
            singleNameTranslation_txt.ResetText();
            pluralNameTranslation_txt.ResetText();
            languages_cbb.SelectedIndex = 0;
            face1_txt.Text = FACE_CODE_ZERO;
            face2_txt.Text = face1_txt.Text;
            reserved_txt.Text = "reserved";
        }

        private void CloseAll_btn_Click(object sender, EventArgs e)
        {
            Control c;
            for (int i = 0; i < 7; i++)
            {
                c = troopPanel.Controls.Find("showGroup_" + (i + 1) + "_btn", false)[0];
                c.Top = 3 + i * 30;
                c.Height = 25;
                c.Text = "v";
            }
            for (int i = 0; i < 7; i++)
            {
                c = troopPanel.Controls.Find("groupBox_" + (i + 1) + "_gb", false)[0];
                c.Top = 2 + i * 30;
                c.Height = 25;
            }
            Height = 428;
        }

        private void Id_txt_TextChanged(object sender, EventArgs e)
        {
            bool isInList = false;
            string id = id_txt.Text.Trim();
            if (id.IndexOf("trp_") != 0)
                id = "trp_" + id;
            for (int i = 0; i < troopSelect_lb.Items.Count; i++)
            {
                if (troopSelect_lb.Items[i].Equals(id))
                {
                    isInList = true;
                    i = troopSelect_lb.Items.Count;
                }
            }
            if (!isInList)
                save_btn.Text = "CREATE";
            else if (save_btn.Text.Equals("CREATE"))
                save_btn.Text = "SAVE";
            if (isInList)
                troopSelect_lb.SelectedIndex = troopSelect_lb.Items.IndexOf(id);
        }

        #endregion

        #region Setups

        private void SetupTroop(Troop troop)
        {
            ResetControls();

            #region Names

            id_txt.Text = troop.ID;
            name_txt.Text = troop.ID;
            plural_name_txt.Text = troop.PluralName;

            #endregion

            #region GROUP1 - Flags & Guarantee

            string flagsS = SkillHunter.Dec2Hex(troop.Flags);
            string skin = "0000000" + flagsS.Substring(7);
            skins_lb.SelectedIndex = int.Parse(SkillHunter.Hex2Dec(skin).ToString());
            if (troop.Flags > 0)
            {
                foreach (string flag in Troop.GetFlagsFromValue(flagsS).Split('|'))
                {
                    Control[] cc = groupBox_1_gb.Controls.Find(flag.Substring(3) + "_cb", false);
                    if (cc.Length == 1 && !flag.Contains("tf_guarantee_"))
                        ((CheckBox)cc[0]).CheckState = CheckState.Checked;
                    else if (cc.Length > 1 && !flag.Contains("tf_guarantee_"))
                        MessageBox.Show("ERROR: Double Flags found! --> " + cc.Length);
                    else
                    {
                        for (int i = 0; i < guarantee_gb.Controls.Count; i++)
                        {
                            CheckBox c = (CheckBox)guarantee_gb.Controls[i];
                            if (c.Name.Remove(c.Name.LastIndexOf('_')).Equals(flag.Replace("tf_guarantee_", string.Empty)))
                            {
                                c.CheckState = CheckState.Checked;
                                i = guarantee_gb.Controls.Count;
                            }
                        }
                    }
                }
            }

            #endregion

            #region GROUP2 - Faction & Special Values

            factions_lb.SelectedIndex = troop.FactionID;
            reserved_txt.Text = troop.Reserved;
            string[] sceneCode = troop.SceneCode.Split('|');
            if (IsNumeric(sceneCode[0]))
                scenes_lb.SelectedIndex = int.Parse(sceneCode[0]);
            else
                scenes_lb.SelectedIndex = 0;
            if (sceneCode.Length > 1)
                entryPoint_numeric.Value = byte.Parse(sceneCode[1]);

            #endregion

            #region GROUP3 - Items

            foreach (int itemID in troop.Items)
                usedItems_lb.Items.Add(itemID + " - " + CodeReader.Items[itemID]);
            inventoryItemFlags = troop.ItemFlags;

            #endregion

            #region GROUP4 - Attributes & Level

            str_txt.Text = troop.Strength.ToString();
            agi_txt.Text = troop.Agility.ToString();
            int_txt.Text = troop.Intelligence.ToString();
            cha_txt.Text = troop.Charisma.ToString();

            level_txt.Text = troop.Level.ToString();

            #endregion

            #region GROUP5 - Proficiencies

            onehandedWeapon_txt.Text = troop.OneHanded.ToString();
            twohandedWeapon_txt.Text = troop.TwoHanded.ToString();
            polearms_txt.Text = troop.Polearm.ToString();
            archery_txt.Text = troop.Archery.ToString();
            crossbows_txt.Text = troop.Crossbow.ToString();
            throwing_txt.Text = troop.Throwing.ToString();
            firearms_txt.Text = troop.Firearm.ToString();

            #endregion

            #region GROUP6 - Skills

            SetupTroopSkills(troop);

            #endregion

            #region GROUP7 - Faces & Upgrade Paths

            face1_txt.Text = troop.Face1;
            face2_txt.Text = troop.Face2;

            if (troop.UpgradeTroop1 < upgradeTroop1_lb.Items.Count)
                upgradeTroop1_lb.SelectedIndex = troop.UpgradeTroop1;
            else
                MessageBox.Show("TROOP_UPGRADE_PATH1:" + Environment.NewLine + troop.UpgradeTroop1ErrorCode);
            if (troop.UpgradeTroop2 < upgradeTroop2_lb.Items.Count)
                upgradeTroop2_lb.SelectedIndex = troop.UpgradeTroop2;
            else
                MessageBox.Show("TROOP_UPGRADE_PATH2:" + Environment.NewLine + troop.UpgradeTroop2ErrorCode);

            #endregion

            #region GROUP8 - Translation

            for (int i = 0; i < languages_cbb.Items.Count; i++)
                PrepareLanguageByIndex(i);
            if (languages_cbb.SelectedIndex != 0)
                languages_cbb.SelectedIndex = 0; // other code is in the SelectedIndex Changed Event
            else
                Language_cbb_SelectedIndexChanged();

            #endregion
        }

        private void SetupTroopSkills(Troop troop)
        {
            for (int i = 0; i < troop.Skills.Length; i++)
            {
                for (int j = 0; j < groupBox_6_gb.Controls.Count; j++)
                {
                    Control c = groupBox_6_gb.Controls[j];
                    if (c.Tag != null && c.Name.Substring(c.Name.LastIndexOf('_')).Equals("_num"))
                    {
                        if (i == int.Parse(c.Tag.ToString()))
                        {
                            ((NumericUpDown)c).Value = troop.Skills[i];
                            j = groupBox_6_gb.Controls.Count;
                        }
                    }
                }
            }
        }

        #endregion

        #region SAVE

        private void Save_btn_Click(object sender, EventArgs e)
        {
            if (troopSelect_lb.SelectedIndex >= 0)
            {
                int index;
                if (save_btn.Text.Equals("SAVE"))
                    index = GetIndexOfTroopByID(troopSelect_lb.SelectedItem.ToString());
                else
                    index = troopSelect_lb.Items.Count;
                SaveTroopByIndex(index);
            }
        }

        private void SaveTroopByIndex(int selectedIndex)
        {
            List<Skriptum> list = new List<Skriptum>();
            string[] values = new string[6];
            string tmp = string.Empty;

            if (troopImage_txt.Text.Length == 0)
                troopImage_txt.Text = "0";
            if (reserved_txt.Text.Length == 0)
                reserved_txt.Text = "0";
            if (face1_txt.Text.Length == 0)
                face1_txt.Text = FACE_CODE_ZERO;
            if (face2_txt.Text.Length == 0)
                face2_txt.Text = FACE_CODE_ZERO;
            if (id_txt.Text.Length > 4)
            {
                if (!id_txt.Text.Substring(0, 4).Equals("trp_"))
                    id_txt.Text = "trp_" + id_txt.Text;
            }
            else
                id_txt.Text = "trp_" + id_txt.Text;

            values[0] = id_txt.Text.Replace(' ', '_') + ' ' + name_txt.Text.Replace(' ', '_') + ' ' + plural_name_txt.Text.Replace(' ', '_') + ' ' + troopImage_txt.Text.Replace(' ', '_') + ' ';
            values[0] += GetFlagsValue().ToString() + ' ' + GetSceneCode().ToString() + ' ' + reserved_txt.Text + ' ';
            values[0] += factions_lb.SelectedIndex.ToString() + ' ' + upgradeTroop1_lb.SelectedIndex.ToString() + ' ' + upgradeTroop2_lb.SelectedIndex.ToString();

            for (int i = 0; i < usedItems_lb.Items.Count; i++)
                tmp += usedItems_lb.Items[i].ToString().Split('-')[0] + inventoryItemFlags[i] + " "; // could be a problem when itemFlags are fucked up

            for (int i = 0; i < (64 - usedItems_lb.Items.Count); i++)
                tmp += "-1 0 ";

            values[1] = "  " + tmp;

            values[2] = "  " + str_txt.Text + ' ' + agi_txt.Text + ' ' + int_txt.Text + ' ' + cha_txt.Text + ' ' + level_txt.Text;

            values[3] = " " + onehandedWeapon_txt.Text + ' ' + twohandedWeapon_txt.Text + ' ' + polearms_txt.Text + ' ' + archery_txt.Text + ' ' + crossbows_txt.Text + ' ' + throwing_txt.Text + ' '
                            + firearms_txt.Text;

            SuperGZ_192Bit skillsValue = new SuperGZ_192Bit(GetSkillCodes());
            tmp = string.Empty;
            foreach (uint u in skillsValue.ValueUInt)
                tmp += u + " ";
            values[4] = tmp; // SKILLS

            SuperGZ_256Bit faceCode1 = new SuperGZ_256Bit(face1_txt.Text);
            SuperGZ_256Bit faceCode2 = new SuperGZ_256Bit(face2_txt.Text);
            tmp = string.Empty;
            for (int i = 0; i < faceCode1.ValueULong.Length; i++)
                tmp += faceCode1.ValueULong[i] + " ";
            for (int i = 0; i < faceCode2.ValueULong.Length; i++)
                tmp += faceCode2.ValueULong[i] + " ";
            values[5] = "  " + tmp; // FACE CODES

            //MessageBox.Show(values[0] + Environment.NewLine + values[1] + Environment.NewLine + values[2] + Environment.NewLine + values[3] + Environment.NewLine + values[4]
            //                + Environment.NewLine + values[5]);

            bool newOne = false;
            Troop changed = new Troop(values);

            MB_Studio.SavePseudoCodeByType(changed, values);

            if (selectedIndex < troopSelect_lb.Items.Count - 1)
                troops[selectedIndex] = changed;
            else
            {
                troops.Add(changed);
                troopSelect_lb.Items.Add(changed.ID);
                newOne = !newOne;
            }

            foreach (Troop troop in troops)
                list.Add(troop);

            SourceWriter.WriteAllObjects();
            new SourceWriter().WriteTroops(list);

            if (newOne)
                troopSelect_lb.SelectedIndex = troopSelect_lb.Items.Count - 1;
        }

        #region Calculations

        private uint GetFlagsValue()
        {
            uint x = 0x00000000;

            if (skins_lb.SelectedIndex > 0)
                x |= (uint)skins_lb.SelectedIndex; // skins

            if (hero_cb.Checked)
                x |= 0x00000010; // tf_hero
            if (inactive_cb.Checked)
                x |= 0x00000020; // tf_inactive
            if (unkillable_cb.Checked)
                x |= 0x00000040; // tf_unkillable
            if (allways_fall_dead_cb.Checked)
                x |= 0x00000080; // tf_allways_fall_dead
            if (no_capture_alive_cb.Checked)
                x |= 0x00000100; // tf_no_capture_alive
            if (mounted_cb.Checked)
                x |= 0x00000400; // tf_mounted
            if (is_merchant_cb.Checked)
                x |= 0x00001000; // tf_is_merchant
            if (randomize_face_cb.Checked)
                x |= 0x00008000; // tf_randomize_face 

            if (unmoveable_in_party_window_cb.Checked)
                x |= 0x10000000; // tf_unmoveable_in_party_window 

            #region Guarantee

            if (boots_cb.Checked)
                x |= 0x00100000; // tf_guarantee_boots
            if (armor_cb.Checked)
                x |= 0x00200000; // tf_guarantee_armor 
            if (helmet_cb.Checked)
                x |= 0x00400000; // tf_guarantee_helmet
            if (gloves_cb.Checked)
                x |= 0x00800000; // tf_guarantee_gloves 
            if (horse_cb.Checked)
                x |= 0x01000000; // tf_guarantee_horse 
            if (shield_cb.Checked)
                x |= 0x02000000; // tf_guarantee_shield 
            if (ranged_cb.Checked)
                x |= 0x04000000; // tf_guarantee_ranged
            if (polearm_cb.Checked)
                x |= 0x08000000; // tf_guarantee_polearm

            #endregion

            return x;
        }

        private uint GetSceneCode()
        {
            uint tsf_entry_mask = 0x00ff0000;
            byte tsf_entry_bits = 16;

            uint scnCode = 0x00000000;
            uint entryPoint = 0;

            if (scenes_lb.SelectedIndex > 0)
                scnCode = (uint)scenes_lb.SelectedIndex;
            if (entryPoint_numeric.Value > 0)
                entryPoint = ((uint)entryPoint_numeric.Value << tsf_entry_bits) & tsf_entry_mask;
            scnCode |= entryPoint;

            return scnCode;
        }

        private uint[] GetSkillCodes()
        {
            uint[] skillCodes = new uint[6];
            string tmp = string.Empty;
            for (int i = 0; i < SkillHunter.Skillnames.Length - 6; i++)
            {
                for (int j = 0; j < groupBox_6_gb.Controls.Count; j++)
                {
                    Control num = groupBox_6_gb.Controls[j];
                    if (num.TabIndex == i && num.Name.Substring(num.Name.LastIndexOf('_') + 1).Equals("num"))
                    {
                        tmp += SkillHunter.Dec2Hex(((NumericUpDown)num).Value).Substring(7);
                        j = groupBox_6_gb.Controls.Count;
                    }
                }
            }
            tmp += "000000"; // maybe replace later if there are more than 42 skills possible
            for (int i = 5; i >= 0; i--)
                skillCodes[i] = uint.Parse(SkillHunter.Hex2Dec(ImportantMethods.ReverseString(tmp.Substring(i * 8, 8))).ToString());
            //MessageBox.Show(skillCode);
            //IEnumerable<string> skillCodes = ImportantMethods.WholeChunks(skillCode, 8);
            //for (int i = 0; i < sss.Length; i++)
            //{
            //    sss[i] = skillCodes.GetEnumerator().Current;
            //    if (i < sss.Length - 1)
            //        skillCodes.GetEnumerator().MoveNext();
            //}
            //MessageBox.Show(SkillHunter.Hex2Dec(sss[0]) + "|" + SkillHunter.Hex2Dec(sss[1]) + "|" + SkillHunter.Hex2Dec(sss[2]) + "|" + SkillHunter.Hex2Dec(sss[3]) + "|" + SkillHunter.Hex2Dec(sss[4]) + "|" + SkillHunter.Hex2Dec(sss[5]));
            return skillCodes;
        }

        #endregion

        #endregion

        #region Items

        private void AddItemToUsedItems_btn_Click(object sender, EventArgs e)
        {
            if ((items_lb.SelectedItems.Count + usedItems_lb.Items.Count) <= 64)
            {
                foreach (string item in items_lb.SelectedItems)
                {
                    AddItemToInventarComboboxByKind(item);
                    usedItems_lb.Items.Add(item);
                }
                /*foreach (string item in items_lb.Items) // FOR TESTING ONLY!!!
                {
                    AddItemToInventarComboboxByKind(item);
                }*/
                inventoryItemFlags.Add(0); // check
            }
            else
                MessageBox.Show("You have too many items selected!"
                                + Environment.NewLine + "Only 64 itemslots are available!"
                                + Environment.NewLine + " --> Used itemslots: " + usedItems_lb.Items.Count
                                + Environment.NewLine + " --> Selected items: " + items_lb.SelectedItems.Count);
        }

        private void AddItemToInventarComboboxByKind(string item)
        {
            Item itemX = null;
            for (int i = 0; i < itemsRList.Length; i++)
            {
                if (itemsRList[i].ID.Equals(item.Split('-')[1].TrimStart()))
                {
                    itemX = itemsRList[i];
                    i = itemsRList.Length;
                }
            }

            if (itemX != null)
            {
                string kind;
                /*try
                {*/
                    kind = itemX.ModBits;
                /*}
                //catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    kind = "ERROR";
                }*/

                if (kind.Contains("_type_"))
                {
                    if (kind.Contains("|"))
                        kind = kind.Substring(kind.IndexOf("itp_type_")).Split('|')[0];
                }
                else
                    kind = "NONE - " + kind;

                Console.WriteLine("TEST - " + kind);

                /*
                itp_type_horse           = 0x0000000000000001 !
                itp_type_one_handed_wpn  = 0x0000000000000002 !
                itp_type_two_handed_wpn  = 0x0000000000000003 !
                itp_type_polearm         = 0x0000000000000004 !
                itp_type_arrows          = 0x0000000000000005 !
                itp_type_bolts           = 0x0000000000000006 !
                itp_type_shield          = 0x0000000000000007 !
                itp_type_bow             = 0x0000000000000008 !
                itp_type_crossbow        = 0x0000000000000009 !
                itp_type_thrown          = 0x000000000000000a !
                itp_type_goods           = 0x000000000000000b ?
                itp_type_head_armor      = 0x000000000000000c !
                itp_type_body_armor      = 0x000000000000000d !
                itp_type_foot_armor      = 0x000000000000000e !
                itp_type_hand_armor      = 0x000000000000000f !
                itp_type_pistol          = 0x0000000000000010 !
                itp_type_musket          = 0x0000000000000011 !
                itp_type_bullets         = 0x0000000000000012 !
                itp_type_animal          = 0x0000000000000013 X
                itp_type_book            = 0x0000000000000014 ?
                */

                if (kind.Equals("itp_type_head_armor"))
                    head_cbb.Items.Add(item);
                
                if (kind.Equals("itp_type_body_armor"))
                    body_cbb.Items.Add(item);

                if (kind.Equals("itp_type_foot_armor"))
                    feet_cbb.Items.Add(item);

                if (kind.Equals("itp_type_one_handed_wpn") || kind.Equals("itp_type_two_handed_wpn") || kind.Equals("itp_type_polearm") || kind.Equals("itp_type_arrows") ||
                    kind.Equals("itp_type_bolts") || kind.Equals("itp_type_bow") || kind.Equals("itp_type_crossbow") || kind.Equals("itp_type_thrown") || kind.Equals("itp_type_pistol") ||
                    kind.Equals("itp_type_musket") || kind.Equals("itp_type_bullets")) // itp_type_goods, itp_type_book, 0
                    weapon_cbb.Items.Add(item);
                
                if (kind.Equals("itp_type_shield"))
                    shield_cbb.Items.Add(item); // if weapon is onehanded

                if (kind.Equals("itp_type_horse"))
                    horse_cbb.Items.Add(item);

                if (kind.Equals("itp_type_animal"))
                {
                    DialogResult dlr = MessageBox.Show("This is marked as animal!" + Environment.NewLine + "Do you really want to add this item?",
                                                        Application.ProductName); // TODO: Add condition for this!
                    if (dlr == DialogResult.Yes)
                        weapon_cbb.Items.Add(item);
                }
            }
        }

        private void UsedItemUP_btn_Click(object sender, EventArgs e)
        {
            if (usedItems_lb.SelectedIndex > 0)
            {
                foreach (int i in usedItems_lb.SelectedIndices)
                {
                    string tmp = usedItems_lb.Items[i - 1].ToString();
                    usedItems_lb.Items[i - 1] = usedItems_lb.Items[i];
                    usedItems_lb.Items[i] = tmp;
                    //usedItems_lb.SelectedIndex -= 1; // rethink this
                }
            }
        }

        private void UsedItemDOWN_btn_Click(object sender, EventArgs e)
        {
            if (usedItems_lb.SelectedIndex < usedItems_lb.Items.Count - 1)
            {
                foreach (int i in usedItems_lb.SelectedIndices)
                {
                    string tmp = usedItems_lb.Items[i + 1].ToString();
                    usedItems_lb.Items[i + 1] = usedItems_lb.Items[i];
                    usedItems_lb.Items[i] = tmp;
                    //usedItems_lb.SelectedIndex += 1; // rethink this
                }
            }
        }

        private void UsedItemREMOVE_btn_Click(object sender, EventArgs e)
        {
            if (usedItems_lb.Items.Count > 0)
            {
                int i = usedItems_lb.SelectedIndex;
                if (i < 0)
                    i = usedItems_lb.Items.Count - 1;
                    usedItems_lb.Items.RemoveAt(i);
                    inventoryItemFlags.RemoveAt(i); // check
            }
        }

        private void SelectedItemFlags_txt_TextChanged(object sender, EventArgs e)
        {
            if (IsNumeric(selectedItemFlags_txt.Text))
                selectedItemFlags_txt.ForeColor = Color.White;
            else
                selectedItemFlags_txt.ForeColor = Color.Red;
            if ((setItemFlags_btn.Enabled && selectedItemFlags_txt.ForeColor == Color.Red)
              ||(!setItemFlags_btn.Enabled && selectedItemFlags_txt.ForeColor == Color.White))
                setItemFlags_btn.Enabled = !setItemFlags_btn.Enabled;
        }

        private void SetItemFlags_btn_Click(object sender, EventArgs e)
        {
            inventoryItemFlags[usedItems_lb.SelectedIndex] = ulong.Parse(selectedItemFlags_txt.Text);
        }

        private void UsedItems_lb_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = usedItems_lb.SelectedIndex;
            if (selectedIndex >= 0)
            {
                if (inventoryItemFlags.Count > selectedIndex)
                    selectedItemFlags_txt.Text = inventoryItemFlags[selectedIndex].ToString();
                LoadCurrentMeshWithOpenBrf((ListBox)sender);
            }
        }

        private void SearchItems_TextChanged(object sender, EventArgs e)
        {
            items_lb.Items.Clear();
            if (!searchItems_SearchTextBox.Text.Contains("Search ...") && searchItems_SearchTextBox.Text.Length > 0)
            {
                foreach (string item in items)
                    if (item.Replace(" ", string.Empty).Split('-')[1].Contains(searchItems_SearchTextBox.Text))
                        items_lb.Items.Add(item);
            }
            else
                foreach (string item in items)
                    items_lb.Items.Add(item);
        }

        private void SearchUsedItems_txt_TextChanged(object sender, EventArgs e)
        {
            usedItems_lb.ClearSelected();
            if (!searchUsedItems_SearchTextBox.Text.Contains("Search ...") && searchUsedItems_SearchTextBox.Text.Length > 0)
            {
                for (int i = 0; i < usedItems_lb.Items.Count; i++)
                    if (usedItems_lb.Items[i].ToString().Replace(" ", string.Empty).Split('-')[1].Contains(searchUsedItems_SearchTextBox.Text))
                        usedItems_lb.SelectedItems.Add(usedItems_lb.Items[i]);
            }
        }

        private void LoadSets()
        {
            string[] names = new string[Properties.Settings.Default.setNames.Count];
            Properties.Settings.Default.setNames.CopyTo(names, 0);
            foreach (Control c in itemSets_gb.Controls)
            {
                string nameEnd = GetNameEndOfControl(c);
                if (nameEnd.Equals("btn"))
                {
                    Invoke((MethodInvoker)delegate
                    {
                        c.Text = names[int.Parse(c.Name.Split('_')[1]) - 1];
                        c.Click += Set_X_Click;
                    });
                }
            }
        }

        private void Set_X_Click(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            int i = int.Parse(c.Name.Split('_')[1]) - 1;
            string[] itemsFromSet = GetItemsFromSetByIndex(i);
            if (itemsFromSet != null)
            {
                usedItems_lb.Items.Clear();
                foreach (string itemID in itemsFromSet)
                    usedItems_lb.Items.Add(items[int.Parse(itemID)]);
            }
            else
                MessageBox.Show(c.Text +  " doesn't have items yet!", "Itemsets", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static string[] GetItemsFromSetByIndex(int index)
        {
            string[] array;
            string[] setXItems = Properties.Settings.Default.setItems[index].Split('|');
            if (!setXItems[0].Equals("-"))
            {
                array = new string[setXItems.Length];
                for (int i = 0; i < array.Length; i++)
                    array[i] = setXItems[i];
            }
            else
                array = null;
            return array;
        }

        public static string[] GetItemsFlagsFromSetByIndex(int index)
        {
            StringCollection setItemsFlags = Properties.Settings.Default.setItemsFlags;
            string[] array = new string[setItemsFlags.Count];
            for (int i = 0; i < array.Length; i++)
                array[i] = setItemsFlags[i];
            return array;
        }

        #endregion

        #region Translation

        private void PrepareLanguageByIndex(int index)
        {
            string[] tmp;
            bool foundTroopSingle = false;
            bool foundTroopPlural = false;
            string filePath = CodeReader.ModPath + GetSecondFilePath(MB_Studio.CSV_FORMAT, GetLanguageFromIndex(index));
            if (File.Exists(filePath))
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    while (!sr.EndOfStream && (!foundTroopSingle || !foundTroopPlural))
                    {
                        tmp = sr.ReadLine().Split('|');
                        if (!foundTroopSingle)
                        {
                            if (tmp[0].Equals(id_txt.Text) && tmp.Length > 1)
                            {
                                translations[index][0] = tmp[1];  //singleNameTranslation_txt.Text = tmp[1];
                                foundTroopSingle = true;
                            }
                        }
                        if (!foundTroopPlural)
                        {
                            if (tmp[0].Equals(id_txt.Text + "_pl") && tmp.Length > 1)
                            {
                                translations[index][1] = tmp[1];  //pluralNameTranslation_txt.Text = tmp[1];
                                foundTroopPlural = true;
                            }
                        }
                    }
                }
            }
            //else
            //    MessageBox.Show("PATH DOESN'T EXIST --> CodeReader.ModPath + GetSecondFilePath(MB_Studio.CSV_FORMAT)" + Environment.NewLine + CodeReader.ModPath + GetSecondFilePath(MB_Studio.CSV_FORMAT));
        }

        private void Language_cbb_SelectedIndexChanged(object sender = null, EventArgs e = null)
        {
            if (translations.Count > 0)
            {
                string filePath = CodeReader.ModPath + GetSecondFilePath(MB_Studio.CSV_FORMAT);
                singleNameTranslation_txt.Text = translations[languages_cbb.SelectedIndex][0];
                pluralNameTranslation_txt.Text = translations[languages_cbb.SelectedIndex][1];
                fileSaver = new FileSaver(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
            }
        }

        private string GetLanguageFromIndex(int index) { return languages_cbb.Items[index].ToString().Split('(')[0].TrimEnd(); }

        private string CurrentLanguage { get { return GetLanguageFromIndex(languages_cbb.SelectedIndex); } }

        private string GetSecondFilePath(string fileFormat, string language = null)
        {
            if (language == null)
                language = CurrentLanguage;
            return "languages\\" + language + "\\troops" + fileFormat;
        }

        private void Save_translation_btn_Click(object sender, EventArgs e)
        {
            List<string> list = new List<string>();
            string id = id_txt.Text.Replace(' ', '_');
            bool singleFound = false;
            bool pluralFound = false;
            string[] orgLines = new string[] { FileSaver.LockItVersion.ToString() };
            if (File.Exists(fileSaver.FilePath))
            {
                orgLines = File.ReadAllLines(fileSaver.FilePath);
                for (int i = 0; i < orgLines.Length; i++)
                {
                    string s = orgLines[i].Split('|')[0];
                    if (!singleFound)
                    {
                        if (s.Equals(id))
                        {
                            orgLines[i] = id + '|' + singleNameTranslation_txt.Text;
                            singleFound = true;
                        }
                    }
                    if (!pluralFound)
                    {
                        if (s.Equals(id + "_pl"))
                        {
                            orgLines[i] = id + "_pl|" + pluralNameTranslation_txt.Text;
                            pluralFound = true;
                        }
                    }
                    if (pluralFound && singleFound)
                        i = orgLines.Length;
                }
            }
            if (!singleFound)
                list.Add(id + "|" + singleNameTranslation_txt.Text);
            if (!pluralFound)
                list.Add(id + "_pl|" + pluralNameTranslation_txt.Text);
            fileSaver.SaveFile(list, ImportantMethods.StringArrayToList(orgLines, 1));
        }

        #endregion

        #region OpenBrf

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (openBrfThread != null)
                KillOpenBrfThread();

            base.OnHandleDestroyed(e);
        }

        //[SecurityPermission(SecurityAction.Demand, ControlThread = true)]
        private void KillOpenBrfThread()
        {
            openBrfManager.Close();
            Console.WriteLine("openBrfThread.IsAlive: " + openBrfThread.IsAlive);
        }

        private void StartOpenBrfManager()
        {
            if (openBrfManager == null && _3DView_btn.Enabled)
                Invoke((MethodInvoker)delegate { _3DView_btn.PerformClick(); });
            else if (_3DView_btn.Enabled)
            {
                Invoke((MethodInvoker)delegate { _3DView_btn.Enabled = false; });
                Thread t = new Thread(new ThreadStart(AddOpenBrfAsChildThread)) { IsBackground = true };
                t.Start();
            }
        }

        private void AddOpenBrfAsChildThread()
        {
            while (!openBrfManager.IsShown)
                Thread.Sleep(10);
            Invoke((MethodInvoker)delegate
            {
                openBrfManager.AddWindowHandleToControlsParent(this); //ImportantMethods.AddWindowHandleToControl(openBrfManager.Handle, Parent, Height, Width, Top);

                Thread.Sleep(50);

                items_lb.SelectedIndex = 0;

                // Update UI
                Invoke(new UpdateUIDelegate(UpdateUI), new object[] { true });

                Console.WriteLine("Loaded 3D View successfully! - laut Programmablauf");
            });
        }

        private void Items_lb_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadCurrentMeshWithOpenBrf((ListBox)sender);
        }

        private void LoadCurrentMeshWithOpenBrf(ListBox lb)
        {
            int selectedIndex = lb.SelectedIndex;
            if (selectedIndex >= 0)
            {
                try
                {
                    string itemID = lb.SelectedItem.ToString().Split('-')[1].TrimStart();
                    itemID = itemID.Substring(itemID.IndexOf('_') + 1);
                    for (int i = 0; i < itemsRList.Length; i++)
                    {
                        if (itemID.Equals(itemsRList[i].ID))
                        {
                            for (int j = 0; j < itemsRList[i].Meshes.Count; j++)
                            {
                                string sss = itemsRList[i].Meshes[j].Split()[0].Trim(); //0 was j
                                Console.WriteLine("|" + sss + "|");
                                Console.WriteLine("SUCCESS: " + openBrfManager.SelectItemNameByKind(sss));
                            }
                            i = itemsRList.Length;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void TestDummyMode(ListBox lb)
        {
            int selectedIndex = lb.SelectedIndex;
            if (selectedIndex >= 0)
            {
                try
                {
                    string itemID = lb.SelectedItem.ToString().Split('-')[1].TrimStart();
                    itemID = itemID.Substring(itemID.IndexOf('_') + 1);
                    for (int i = 0; i < itemsRList.Length; i++)
                    {
                        if (itemID.Equals(itemsRList[i].ID))
                        {
                            for (int j = 0; j < itemsRList[i].Meshes.Count; j++)
                            {
                                string sss = itemsRList[i].Meshes[j].Split()[0].Trim();
                                Console.WriteLine("|" + sss + "|");
                                Console.WriteLine("TEST - DUMMY MODE!");
                                openBrfManager.AddMeshToTroopDummy(sss);
                            }
                            i = itemsRList.Length;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void ShowGroup_3_btn_Click(object sender, EventArgs e)
        {
            if (showGroup_3_btn.Text.Equals("v"))
                StartOpenBrfManager();
        }

        private void _3DView_btn_Click(object sender, EventArgs e)
        {
            if (openBrfManager == null && _3DView_btn.Enabled)
            {
                _3DView_btn.Text = _3DView_btn.Text.Remove(_3DView_btn.Text.LastIndexOf(' ')) + " Enabled";
                _3DView_btn.Visible = false;

                string mabPath = ProgramConsole.GetModuleInfoPath();
                mabPath = mabPath.Remove(mabPath.IndexOf('%')).TrimEnd('\\');
                mabPath = mabPath.Remove(mabPath.LastIndexOf('\\'));
                openBrfManager = new OpenBrfManager(ProgramConsole.OriginalMod, mabPath);

                showGroup_3_btn.PerformClick();

                Console.WriteLine("DEBUGMODE: " + MB_Studio.DebugMode);
                int result = openBrfManager.Show(MB_Studio.DebugMode);
                Console.WriteLine("OPENBRF_EXIT_CODE:" + result);
            }
        }

        #endregion

        private void Button1_Click(object sender, EventArgs e)
        {
            TestDummyMode(items_lb);
        }
    }
}