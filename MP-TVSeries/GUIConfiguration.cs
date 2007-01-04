using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using MediaPortal.Util;
using System.Windows.Forms;
using WindowPlugins.GUITVSeries;
#if DEBUG
using System.Diagnostics;
#endif

namespace WindowPlugins.GUITVSeries
{
    public partial class ConfigurationForm : Form, Feedback.Interface
    {
        private List<Panel> m_paneList = new List<Panel>();
        private TreeNode nodeEdited = null;
        private OnlineParsing m_parser = null;
        private DateTime m_timingStart = new DateTime();

        private DBSeries m_SeriesReference = new DBSeries(true);
        private DBSeason m_SeasonReference = new DBSeason();
        private DBEpisode m_EpisodeReference = new DBEpisode(true);

        private DBTorrentSearch m_currentTorrentSearch = null;

        public ConfigurationForm()
        {
#if DEBUG
            //    Debugger.Launch();
#endif
            InitializeComponent();
            MPTVSeriesLog.AddNotifier(ref listBox_Log);

            MPTVSeriesLog.Write("**** Plugin started in configuration mode ***");
            InitSettingsTreeAndPanes();
            LoadImportPathes();
            LoadExpressions();
            LoadReplacements();
            LoadTree();
        }

        #region Init
        private void InitSettingsTreeAndPanes()
        {
            this.comboBox1.SelectedIndex = 0;

            // temp: remove the subtitle tab for now, not ready for prime time (if it ever is :) )
//            tabControl_Details.Controls.Remove(tabpage_Subtitles);

            m_paneList.Add(panel_ImportPathes);
            m_paneList.Add(panel_Expressions);
            m_paneList.Add(panel_StringReplacements);
            m_paneList.Add(panel_ParsingTest);
            m_paneList.Add(panel_OnlineData);

            foreach (Panel pane in m_paneList)
            {
                pane.Dock = DockStyle.Fill;
                pane.Visible = false;
                TreeNode node = new TreeNode(pane.Tag.ToString());
                node.Name = pane.Name;
                treeView_Settings.Nodes.Add(node);
            }

            splitContainer1.Panel2Collapsed = DBOption.GetOptions(DBOption.cConfig_LogCollapsed);
            treeView_Settings.SelectedNode = treeView_Settings.Nodes[0];
            textBox_PluginHomeName.Text = DBOption.GetOptions(DBOption.cView_PluginName);
            checkBox_OnlineSearch.Checked = DBOption.GetOptions(DBOption.cOnlineParseEnabled);
            checkBox_FullSeriesRetrieval.Checked = DBOption.GetOptions(DBOption.cFullSeriesRetrieval);
            checkBox_AutoChooseSeries.Checked = DBOption.GetOptions(DBOption.cAutoChooseSeries);
            checkBox_LocalDataOverride.Checked = DBOption.GetOptions(DBOption.cLocalDataOverride);
            checkBox_Episode_OnlyShowLocalFiles.Checked = DBOption.GetOptions(DBOption.cView_Episode_OnlyShowLocalFiles);
            checkBox_Episode_HideUnwatchedSummary.Checked = DBOption.GetOptions(DBOption.cView_Episode_HideUnwatchedSummary);

            checkBox_ShowHidden.Checked = DBOption.GetOptions(DBOption.cShowHiddenItems);
            checkBox_DontClearMissingLocalFiles.Checked = DBOption.GetOptions(DBOption.cDontClearMissingLocalFiles);
            checkBox_AutoOnlineDataRefresh.Checked = DBOption.GetOptions(DBOption.cAutoUpdateOnlineData);
            numericUpDown_AutoOnlineDataRefresh.Enabled = checkBox_AutoOnlineDataRefresh.Checked;
            int nValue = DBOption.GetOptions(DBOption.cAutoUpdateOnlineDataLapse);
            numericUpDown_AutoOnlineDataRefresh.Minimum = 1;
            numericUpDown_AutoOnlineDataRefresh.Maximum = 24;
            numericUpDown_AutoOnlineDataRefresh.Value = nValue;

            checkBox_RandBanner.Checked = DBOption.GetOptions(DBOption.cRandomBanner);

            checkBox_AutoHeight.Checked = DBOption.GetOptions(DBOption.cViewAutoHeight);
            comboBox_seriesFormat.Items.Add("Text");
            comboBox_seriesFormat.Items.Add("Graphical");
            comboBox_seriesFormat.SelectedIndex = DBOption.GetOptions(DBOption.cView_Series_ListFormat);
            richTextBox_seriesFormat_Col1.Tag = new FieldTag(DBOption.cView_Series_Col1, FieldTag.Level.Series);
            FieldValidate(ref richTextBox_seriesFormat_Col1);

            richTextBox_seriesFormat_Col2.Tag = new FieldTag(DBOption.cView_Series_Col2, FieldTag.Level.Series);
            FieldValidate(ref richTextBox_seriesFormat_Col2);

            richTextBox_seriesFormat_Col3.Tag = new FieldTag(DBOption.cView_Series_Col3, FieldTag.Level.Series);
            FieldValidate(ref richTextBox_seriesFormat_Col3);

            richTextBox_seriesFormat_Title.Tag = new FieldTag(DBOption.cView_Series_Title, FieldTag.Level.Series);
            FieldValidate(ref richTextBox_seriesFormat_Title);

            richTextBox_seriesFormat_Subtitle.Tag = new FieldTag(DBOption.cView_Series_Subtitle, FieldTag.Level.Series);
            FieldValidate(ref richTextBox_seriesFormat_Subtitle);

            richTextBox_seriesFormat_Main.Tag = new FieldTag(DBOption.cView_Season_Main, FieldTag.Level.Series);
            FieldValidate(ref richTextBox_seriesFormat_Main);

            comboBox_seasonFormat.Items.Add("Text");
            comboBox_seasonFormat.Items.Add("Graphical");
            comboBox_seasonFormat.SelectedIndex = DBOption.GetOptions(DBOption.cView_Season_ListFormat);

            richTextBox_seasonFormat_Col1.Tag = new FieldTag(DBOption.cView_Season_Col1, FieldTag.Level.Season);
            FieldValidate(ref richTextBox_seasonFormat_Col1);

            richTextBox_seasonFormat_Col2.Tag = new FieldTag(DBOption.cView_Season_Col2, FieldTag.Level.Season);
            FieldValidate(ref richTextBox_seasonFormat_Col2);

            richTextBox_seasonFormat_Col3.Tag = new FieldTag(DBOption.cView_Season_Col3, FieldTag.Level.Season);
            FieldValidate(ref richTextBox_seasonFormat_Col3);

            richTextBox_seasonFormat_Title.Tag = new FieldTag(DBOption.cView_Season_Title, FieldTag.Level.Season);
            FieldValidate(ref richTextBox_seasonFormat_Title);

            richTextBox_seasonFormat_Subtitle.Tag = new FieldTag(DBOption.cView_Season_Subtitle, FieldTag.Level.Season);
            FieldValidate(ref richTextBox_seasonFormat_Subtitle);

            richTextBox_seasonFormat_Main.Tag = new FieldTag(DBOption.cView_Season_Main, FieldTag.Level.Season);
            FieldValidate(ref richTextBox_seasonFormat_Main);

            richTextBox_episodeFormat_Col1.Tag = new FieldTag(DBOption.cView_Episode_Col1, FieldTag.Level.Episode);
            FieldValidate(ref richTextBox_episodeFormat_Col1);

            richTextBox_episodeFormat_Col2.Tag = new FieldTag(DBOption.cView_Episode_Col2, FieldTag.Level.Episode);
            FieldValidate(ref richTextBox_episodeFormat_Col2);

            richTextBox_episodeFormat_Col3.Tag = new FieldTag(DBOption.cView_Episode_Col3, FieldTag.Level.Episode);
            FieldValidate(ref richTextBox_episodeFormat_Col3);

            richTextBox_episodeFormat_Title.Tag = new FieldTag(DBOption.cView_Episode_Title, FieldTag.Level.Episode);
            FieldValidate(ref richTextBox_episodeFormat_Title);

            richTextBox_episodeFormat_Subtitle.Tag = new FieldTag(DBOption.cView_Episode_Subtitle, FieldTag.Level.Episode);
            FieldValidate(ref richTextBox_episodeFormat_Subtitle);

            richTextBox_episodeFormat_Main.Tag = new FieldTag(DBOption.cView_Episode_Main, FieldTag.Level.Episode);
            FieldValidate(ref richTextBox_episodeFormat_Main);

            textBox_foromBaseURL.Text = DBOption.GetOptions(DBOption.cSubs_Forom_BaseURL);
            textBox_foromID.Text = DBOption.GetOptions(DBOption.cSubs_Forom_ID);

            minHDHeight.Text = DBOption.GetOptions("minHDHeight");
            minHDWidth.Text = DBOption.GetOptions("minHDWidth");

            LoadTorrentSearches();
        }

        private void LoadTorrentSearches()
        {
            textBox_uTorrentPath.Text = DBOption.GetOptions(DBOption.cUTorrentPath);

            List<DBTorrentSearch> torrentSearchList = DBTorrentSearch.Get();
            foreach (DBTorrentSearch item in torrentSearchList)
            {
                comboBox_TorrentPreset.Items.Add(item);
                if (item[DBTorrentSearch.cID] == DBOption.GetOptions(DBOption.cTorrentSearch))
                    m_currentTorrentSearch = item;
            }
            if (m_currentTorrentSearch != null)
                comboBox_TorrentPreset.SelectedItem = m_currentTorrentSearch;
            else
                comboBox_TorrentPreset.SelectedIndex = 0;
        }

        private void LoadImportPathes()
        {
            if (dataGridView_ImportPathes.Columns.Count == 0)
            {
                DataGridViewCheckBoxColumn columnEnabled = new DataGridViewCheckBoxColumn();
                columnEnabled.Name = DBImportPath.cEnabled;
                columnEnabled.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
                dataGridView_ImportPathes.Columns.Add(columnEnabled);

                DataGridViewButtonColumn columnPath = new DataGridViewButtonColumn();
                columnPath.Name = DBImportPath.cPath;
                columnPath.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dataGridView_ImportPathes.Columns.Add(columnPath);
            }

            DBImportPath[] importPathes = DBImportPath.GetAll();

            dataGridView_ImportPathes.Rows.Clear();

            if (importPathes != null && importPathes.Length > 0)
            {
                dataGridView_ImportPathes.Rows.Add(importPathes.Length);
                foreach (DBImportPath importPath in importPathes)
                {
                    DataGridViewRow row = dataGridView_ImportPathes.Rows[importPath[DBImportPath.cIndex]];
                    row.Cells[DBImportPath.cEnabled].Value = (Boolean)importPath[DBImportPath.cEnabled];
                    row.Cells[DBImportPath.cPath].Value = (String)importPath[DBImportPath.cPath];
                }
            }
        }

        private void LoadExpressions()
        {
            DBExpression[] expressions = DBExpression.GetAll();
            // load them up in the datagrid

            //             foreach (KeyValuePair<string, DBField> field in expressions[0].m_fields)
            //             {
            //                 if (field.Key != DBExpression.cIndex)
            //                 {
            //                     DataGridViewCheckBoxColumn column = new DataGridBoolColumn();
            //                     column.Name = field.Key;
            //                     column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            //                     dataGridView_Expressions.Columns.Add(column);
            //                 }
            //             }

            if (dataGridView_Expressions.Columns.Count == 0)
            {
                DataGridViewCheckBoxColumn columnEnabled = new DataGridViewCheckBoxColumn();
                columnEnabled.Name = DBExpression.cEnabled;
                columnEnabled.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
                dataGridView_Expressions.Columns.Add(columnEnabled);

                DataGridViewComboBoxColumn columnType = new DataGridViewComboBoxColumn();
                columnType.Name = DBExpression.cType;
                columnType.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                DataGridViewComboBoxCell comboCellTemplate = new DataGridViewComboBoxCell();
                comboCellTemplate.Items.Add(DBExpression.cType_Simple);
                comboCellTemplate.Items.Add(DBExpression.cType_Regexp);
                columnType.CellTemplate = comboCellTemplate;
                dataGridView_Expressions.Columns.Add(columnType);

                DataGridViewTextBoxColumn columnExpression = new DataGridViewTextBoxColumn();
                columnExpression.Name = DBExpression.cExpression;
                columnExpression.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dataGridView_Expressions.Columns.Add(columnExpression);
            }
            dataGridView_Expressions.Rows.Clear();
            dataGridView_Expressions.Rows.Add(expressions.Length);

            foreach (DBExpression expression in expressions)
            {
                DataGridViewRow row = dataGridView_Expressions.Rows[expression[DBExpression.cIndex]];
                row.Cells[DBExpression.cEnabled].Value = (Boolean)expression[DBExpression.cEnabled];
                DataGridViewComboBoxCell comboCell = new DataGridViewComboBoxCell();
                comboCell.Items.Add(DBExpression.cType_Simple);
                comboCell.Items.Add(DBExpression.cType_Regexp);
                comboCell.Value = (String)expression[DBExpression.cType];
                row.Cells[DBExpression.cType] = comboCell;
                row.Cells[DBExpression.cExpression].Value = (String)expression[DBExpression.cExpression];
            }
        }

        private void LoadReplacements()
        {
            DBReplacements[] replacements = DBReplacements.GetAll();

            // load them up in the datagrid

            //             foreach (KeyValuePair<string, DBField> field in expressions[0].m_fields)
            //             {
            //                 if (field.Key != DBExpression.cIndex)
            //                 {
            //                     DataGridViewCheckBoxColumn column = new DataGridBoolColumn();
            //                     column.Name = field.Key;
            //                     column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            //                     dataGridView_Expressions.Columns.Add(column);
            //                 }
            //             }

            if (dataGridView_Replace.Columns.Count == 0)
            {
                DataGridViewCheckBoxColumn columnEnabled = new DataGridViewCheckBoxColumn();
                columnEnabled.Name = DBReplacements.cEnabled;
                columnEnabled.HeaderText = DBReplacements.PrettyFieldName(DBReplacements.cEnabled);
                columnEnabled.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
                dataGridView_Replace.Columns.Add(columnEnabled);

                DataGridViewTextBoxColumn columnToReplace = new DataGridViewTextBoxColumn();
                columnToReplace.Name = DBReplacements.cToReplace;
                columnToReplace.HeaderText = DBReplacements.PrettyFieldName(DBReplacements.cToReplace);
                columnToReplace.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dataGridView_Replace.Columns.Add(columnToReplace);

                DataGridViewTextBoxColumn columnWith = new DataGridViewTextBoxColumn();
                columnWith.Name = DBReplacements.cWith;
                columnWith.HeaderText = DBReplacements.PrettyFieldName(DBReplacements.cWith);
                columnWith.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dataGridView_Replace.Columns.Add(columnWith);
            }
            dataGridView_Replace.Rows.Clear();
            dataGridView_Replace.Rows.Add(replacements.Length);

            foreach (DBReplacements replacement in replacements)
            {
                DataGridViewRow row = dataGridView_Replace.Rows[replacement[DBReplacements.cIndex]];
                row.Cells[DBReplacements.cEnabled].Value = (Boolean)replacement[DBReplacements.cEnabled];
                row.Cells[DBReplacements.cToReplace].Value = (String)replacement[DBReplacements.cToReplace];
                row.Cells[DBReplacements.cWith].Value = (String)replacement[DBReplacements.cWith];
            }
        }

        private void LoadTree()
        {
            TreeView root = this.treeView_Library;
            root.Nodes.Clear();

            SQLCondition condition = new SQLCondition();
            condition.Add(new DBSeries(), DBSeries.cDuplicateLocalName, 0, SQLConditionType.Equal);
            if (!DBOption.GetOptions(DBOption.cShowHiddenItems))
                condition.Add(new DBSeries(), DBSeries.cHidden, 0, SQLConditionType.Equal);

            List<DBSeries> seriesList = DBSeries.Get(condition);
            if (seriesList.Count == 0)
            {
                return;
            }

            foreach (DBSeries series in seriesList)
            {
                TreeNode seriesNode = new TreeNode(series[DBOnlineSeries.cPrettyName]);
                seriesNode.Name = DBSeries.cTableName;
                seriesNode.Tag = (DBSeries)series;
                seriesNode.Expand();
                root.Nodes.Add(seriesNode);
                if (series[DBSeries.cHidden])
                {
                    Font fontDefault = treeView_Library.Font;
                    seriesNode.NodeFont = new Font(fontDefault.Name, fontDefault.Size, FontStyle.Italic);
                }

                List<DBSeason> seasonsList = DBSeason.Get(series[DBSeries.cID], false, true, DBOption.GetOptions(DBOption.cShowHiddenItems));
                foreach (DBSeason season in seasonsList)
                {
                    TreeNode seasonNode = new TreeNode("Season " + season[DBSeason.cIndex]);
                    seasonNode.Name = DBSeason.cTableName;
                    seasonNode.Tag = (DBSeason)season;
                    seriesNode.Nodes.Add(seasonNode);
                    // default a season node to disabled, reenable it if an episode node is valid
                    seasonNode.ForeColor = System.Drawing.SystemColors.GrayText;
                    if (season[DBSeason.cHidden])
                    {
                        Font fontDefault = treeView_Library.Font;
                        seasonNode.NodeFont = new Font(fontDefault.Name, fontDefault.Size, FontStyle.Italic);
                    }

                    List<DBEpisode> episodesList = DBEpisode.Get(series[DBSeries.cID], season[DBSeason.cIndex], false, DBOption.GetOptions(DBOption.cShowHiddenItems));

                    foreach (DBEpisode episode in episodesList)
                    {
                        String sEpisodeName = (String)episode[DBEpisode.cEpisodeName];
                        TreeNode episodeNode = new TreeNode(episode[DBEpisode.cSeasonIndex] + "x" + episode[DBEpisode.cEpisodeIndex] + " - " + sEpisodeName);
                        episodeNode.Name = DBEpisode.cTableName;
                        episodeNode.Tag = (DBEpisode)episode;
                        if (episode[DBEpisode.cFilename] == "")
                        {
                            episodeNode.ForeColor = System.Drawing.SystemColors.GrayText;
                        }
                        else
                        {
                            seasonNode.ForeColor = treeView_Library.ForeColor;
                        }
                        if (episode[DBOnlineEpisode.cHidden])
                        {
                            Font fontDefault = treeView_Library.Font;
                            episodeNode.NodeFont = new Font(fontDefault.Name, fontDefault.Size, FontStyle.Italic);
                        }

                        seasonNode.Nodes.Add(episodeNode);
                    }
                    if (episodesList.Count == 0)
                    {
                        // no episodes => no season node
                        seriesNode.Nodes.Remove(seasonNode);
                    }
                }
            }
        }
        #endregion

        #region Import Handling
        private void dataGridView_ImportPathes_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            DBImportPath importPath = new DBImportPath();
            importPath[DBImportPath.cIndex] = e.RowIndex.ToString();
            foreach (DataGridViewCell cell in dataGridView_ImportPathes.Rows[e.RowIndex].Cells)
            {
                if (cell.Value == null)
                    return;
                if (cell.ValueType == typeof(Boolean))
                    importPath[cell.OwningColumn.Name] = (Boolean)cell.Value;
                else
                    importPath[cell.OwningColumn.Name] = (String)cell.Value;
            }
            importPath.Commit();
        }

        private void dataGridView_ImportPathes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView_ImportPathes.Columns[DBImportPath.cPath].Index)
            {
                if (dataGridView_ImportPathes.NewRowIndex == e.RowIndex)
                {
                    dataGridView_ImportPathes.Rows.Add();
                    dataGridView_ImportPathes.Rows[e.RowIndex].Cells[DBImportPath.cEnabled].Value = true;
                }

                if (dataGridView_ImportPathes.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
                    folderBrowserDialog1.SelectedPath = dataGridView_ImportPathes.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                DialogResult result = this.folderBrowserDialog1.ShowDialog();
                if (result.ToString() == "Cancel")
                    return;

                dataGridView_ImportPathes.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = folderBrowserDialog1.SelectedPath;
            }
        }

        private void SaveAllImportPathes()
        {
            // need to save back all the rows
            DBImportPath.ClearAll();

            foreach (DataGridViewRow row in dataGridView_ImportPathes.Rows)
            {
                if (row.Index != dataGridView_ImportPathes.NewRowIndex)
                {
                    DBImportPath importPath = new DBImportPath();
                    importPath[DBImportPath.cIndex] = row.Index.ToString();
                    foreach (DataGridViewCell cell in row.Cells)
                        if (cell.ValueType.Name == "Boolean")
                            importPath[cell.OwningColumn.Name] = (Boolean)cell.Value;
                        else
                            importPath[cell.OwningColumn.Name] = (String)cell.Value;
                    importPath.Commit();
                }
            }
        }

        private void dataGridView_ImportPathes_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            SaveAllImportPathes();
        }
        #endregion

        #region Expressions Handling
        private void dataGridView_Expressions_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DBExpression expression = new DBExpression();
            expression[DBExpression.cIndex] = e.RowIndex.ToString();
            foreach (DataGridViewCell cell in dataGridView_Expressions.Rows[e.RowIndex].Cells)
            {
                if (cell.Value == null)
                    return;
                if (cell.ValueType.Name == "Boolean")
                    expression[cell.OwningColumn.Name] = (Boolean)cell.Value;
                else
                    expression[cell.OwningColumn.Name] = (String)cell.Value;
            }
            expression.Commit();
        }

        private void SaveAllExpressions()
        {
            // need to save back all the rows
            DBExpression.ClearAll();

            foreach (DataGridViewRow row in dataGridView_Expressions.Rows)
            {
                if (row.Index != dataGridView_Expressions.NewRowIndex)
                {
                    DBExpression expression = new DBExpression();
                    expression[DBExpression.cIndex] = row.Index.ToString();
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.Value == null)
                            return;
                        if (cell.ValueType.Name == "Boolean")
                            expression[cell.OwningColumn.Name] = (Boolean)cell.Value;
                        else
                            expression[cell.OwningColumn.Name] = (String)cell.Value;
                    }
                }
            }
        }

        private void dataGridView_Expressions_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            SaveAllExpressions();
        }

        private void button_MoveExpUp_Click(object sender, EventArgs e)
        {
            int nCurrentRow = dataGridView_Expressions.CurrentCellAddress.Y;
            if (nCurrentRow > 0)
            {
                DBExpression expressionGoingUp = new DBExpression(nCurrentRow);
                DBExpression expressionGoingDown = new DBExpression(nCurrentRow - 1);
                expressionGoingUp[DBExpression.cIndex] = Convert.ToString(nCurrentRow - 1);
                expressionGoingUp.Commit();
                expressionGoingDown[DBExpression.cIndex] = Convert.ToString(nCurrentRow);
                expressionGoingDown.Commit();
                LoadExpressions();
                dataGridView_Expressions.CurrentCell = dataGridView_Expressions.Rows[nCurrentRow - 1].Cells[dataGridView_Expressions.CurrentCellAddress.X];

            }
        }

        private void button_MoveExpDown_Click(object sender, EventArgs e)
        {
            int nCurrentRow = dataGridView_Expressions.CurrentCellAddress.Y;
            if (nCurrentRow < dataGridView_Expressions.Rows.Count - 2) //don't take in account the new line 
            {
                DBExpression expressionGoingDown = new DBExpression(nCurrentRow);
                DBExpression expressionGoingUp = new DBExpression(nCurrentRow + 1);
                expressionGoingUp[DBExpression.cIndex] = Convert.ToString(nCurrentRow);
                expressionGoingUp.Commit();
                expressionGoingDown[DBExpression.cIndex] = Convert.ToString(nCurrentRow + 1);
                expressionGoingDown.Commit();
                LoadExpressions();
                dataGridView_Expressions.CurrentCell = dataGridView_Expressions.Rows[nCurrentRow + 1].Cells[dataGridView_Expressions.CurrentCellAddress.X];
            }
        }
        #endregion

        #region Replacements Handling
        private void dataGridView_Replace_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DBReplacements replacement = new DBReplacements();
            replacement[DBReplacements.cIndex] = e.RowIndex.ToString();
            foreach (DataGridViewCell cell in dataGridView_Replace.Rows[e.RowIndex].Cells)
            {
                if (cell.Value == null)
                    return;
                if (cell.ValueType.Name == "Boolean")
                    replacement[cell.OwningColumn.Name] = (Boolean)cell.Value;
                else
                    replacement[cell.OwningColumn.Name] = (String)cell.Value;
            }
            replacement.Commit();
        }

        private void SaveAllReplacements()
        {
            // need to save back all the rows
            DBReplacements.ClearAll();

            foreach (DataGridViewRow row in dataGridView_Expressions.Rows)
            {
                if (row.Index != dataGridView_Expressions.NewRowIndex)
                {
                    DBReplacements replacement = new DBReplacements();
                    replacement[DBReplacements.cIndex] = row.Index.ToString();
                    foreach (DataGridViewCell cell in row.Cells)
                        replacement[cell.OwningColumn.Name] = (String)cell.Value;
                    replacement.Commit();
                }
            }
        }

        private void dataGridView_Replace_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            SaveAllReplacements();
        }
        #endregion

        #region Test Parsing Handling
        void TestParsing_FillList(List<parseResult> results)
        {
            foreach (parseResult progress in results)
            {
                foreach (KeyValuePair<String, String> MatchPair in progress.parser.Matches)
                {
                    if (!listView_ParsingResults.Columns.ContainsKey(MatchPair.Key))
                    {
                        // add a column for that match
                        ColumnHeader newcolumn = new ColumnHeader();
                        newcolumn.Name = MatchPair.Key;
                        newcolumn.Text = MatchPair.Key;
                        listView_ParsingResults.Columns.Add(newcolumn);
                    }
                }

                ListViewItem item = new ListViewItem(progress.match_filename);
                item.SubItems[0].Name = listView_ParsingResults.Columns[0].Name;



                foreach (ColumnHeader column in listView_ParsingResults.Columns)
                {
                    if (column.Index > 0)
                    {
                        ListViewItem.ListViewSubItem subItem = null;
                        if (progress.parser.Matches.ContainsKey(column.Name))
                            subItem = new ListViewItem.ListViewSubItem(item, progress.parser.Matches[column.Name]);
                        else
                            subItem = new ListViewItem.ListViewSubItem(item, "");
                        subItem.Name = column.Name;
                        item.SubItems.Add(subItem);
                    }
                }

                if (progress.failedSeason)
                {
                    item.UseItemStyleForSubItems = false;
                    item.SubItems[DBEpisode.cSeasonIndex].ForeColor = System.Drawing.Color.White;
                    item.SubItems[DBEpisode.cSeasonIndex].BackColor = System.Drawing.Color.Tomato;
                }

                if (progress.failedEpisode)
                {
                    item.UseItemStyleForSubItems = false;
                    item.SubItems[DBEpisode.cEpisodeIndex].ForeColor = System.Drawing.Color.White;
                    item.SubItems[DBEpisode.cEpisodeIndex].BackColor = System.Drawing.Color.Tomato;
                }

                if (!progress.success && !progress.failedEpisode && !progress.failedSeason)
                {
                    item.ForeColor = System.Drawing.Color.White;
                    item.BackColor = System.Drawing.Color.Tomato;
                }

                if (!progress.success)
                    MPTVSeriesLog.Write("Parsing failed for " + progress.match_filename);
                if (progress.failedSeason || progress.failedEpisode)
                    MPTVSeriesLog.Write(progress.exception + " for " + progress.match_filename);
                listView_ParsingResults.Items.Add(item);
                listView_ParsingResults.EnsureVisible(listView_ParsingResults.Items.Count - 1);
                // only do that once in a while, it's really slow
            }
            listView_ParsingResults.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

            foreach (ColumnHeader header in listView_ParsingResults.Columns)
            {
                header.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                header.Width += 10;
                if (header.Width < 80)
                    header.Width = 80;
            }
        }

        void TestParsing_LocalParseCompleted(List<parseResult> results)
        {
            TestParsing_FillList(results);
            MPTVSeriesLog.Write("Parsing test completed");
            this.progressBar_Parsing.Value = 100;
        }

        void TestParsing_LocalParseProgress(int nProgress, List<parseResult> results)
        {
            this.progressBar_Parsing.Value = nProgress;
            TestParsing_FillList(results);
        }

        void TestParsing_Start(bool bForceRefresh)
        {
            if (!bForceRefresh && listView_ParsingResults.Items.Count > 0)
                return;

            listView_ParsingResults.Items.Clear();
            listView_ParsingResults.Columns.Clear();
            // add mandatory columns
            ColumnHeader columnFileName = new ColumnHeader();
            columnFileName.Name = "FileName";
            columnFileName.Text = "FileName";
            listView_ParsingResults.Columns.Add(columnFileName);

            ColumnHeader columnSeriesName = new ColumnHeader();
            columnSeriesName.Name = DBSeries.cParsedName;
            columnSeriesName.Text = "Parsed Series Name";
            listView_ParsingResults.Columns.Add(columnSeriesName);

            ColumnHeader columnSeasonNumber = new ColumnHeader();
            columnSeasonNumber.Name = DBEpisode.cSeasonIndex;
            columnSeasonNumber.Text = "Season ID";
            listView_ParsingResults.Columns.Add(columnSeasonNumber);

            ColumnHeader columnEpisodeNumber = new ColumnHeader();
            columnEpisodeNumber.Name = DBEpisode.cEpisodeIndex;
            columnEpisodeNumber.Text = "Episode ID";
            listView_ParsingResults.Columns.Add(columnEpisodeNumber);

            ColumnHeader columnEpisodeTitle = new ColumnHeader();
            columnEpisodeTitle.Name = DBEpisode.cEpisodeName;
            columnEpisodeTitle.Text = "Episode Title";
            listView_ParsingResults.Columns.Add(columnEpisodeTitle);

            MPTVSeriesLog.Write("Parsing test beginning, getting all files...");

            LocalParse runner = new LocalParse();
            runner.LocalParseProgress += new LocalParse.LocalParseProgressHandler(TestParsing_LocalParseProgress);
            runner.LocalParseCompleted += new LocalParse.LocalParseCompletedHandler(TestParsing_LocalParseCompleted);
            runner.AsyncFullParse();
        }
        #endregion


        private void Parsing_Start()
        {
            if (m_parser != null)
            {
                m_parser.Cancel();
                button_Start.Enabled = false;
            }
            else
            {
                button_Start.Text = "Abort";
                m_timingStart = DateTime.Now;
                m_parser = new OnlineParsing(this);
                m_parser.OnlineParsingProgress += new OnlineParsing.OnlineParsingProgressHandler(runner_OnlineParsingProgress);
                m_parser.OnlineParsingCompleted += new OnlineParsing.OnlineParsingCompletedHandler(runner_OnlineParsingCompleted);
                m_parser.Start(new CParsingParameters(true, true));
            }
        }

        void runner_OnlineParsingProgress(int nProgress)
        {
            this.progressBar_Parsing.Value = nProgress;
        }

        void runner_OnlineParsingCompleted(bool bDataUpdated)
        {
            this.progressBar_Parsing.Value = 100;
            TimeSpan span = DateTime.Now - m_timingStart;
            MPTVSeriesLog.Write("Parsing Completed in " + span);
            button_Start.Text = "Start Import";
            button_Start.Enabled = true;
            m_parser = null;
            // a full configuration scan counts as a scan - set the dates so we don't rescan everything right away in MP
//            DBOption.SetOptions(DBOption.cLocalScanLastTime, DateTime.Now.ToString());
            DBOption.SetOptions(DBOption.cUpdateScanLastTime, DateTime.Now.ToString());

            LoadTree();
        }

        #region Series treeview handling
        private void treeView_Library_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //////////////////////////////////////////////////////////////////////////////
            #region Clears all fields so new data can be entered

            this.detailsPropertyBindingSource.Clear();
            try
            {
                if (this.pictureBox_Series.Image != null)
                {
                    this.pictureBox_Series.Image.Dispose();
                    this.pictureBox_Series.Image = null;
                }
            }
            catch { }

            #endregion
            //////////////////////////////////////////////////////////////////////////////

            //////////////////////////////////////////////////////////////////////////////
            #region Select appropriate tab base on which node level was clicked

            TreeNode node = e.Node;
            switch (node.Name)
            {
                //////////////////////////////////////////////////////////////////////////////
                #region When Episode Nodes is Clicked

                case DBEpisode.cTableName:
                    {
                        DBEpisode episode = (DBEpisode)node.Tag;
                        // assume an episode is always in a season which is always in a series
                        DBSeries series = (DBSeries)node.Parent.Parent.Tag;
                        String filename = series.Banner;
                        if (filename != String.Empty)
                            try
                            {
                                this.pictureBox_Series.Image = Image.FromFile(filename);
                            }
                            catch (Exception)
                            {
                            }

                        comboBox_BannerSelection.Items.Clear();
                        comboBox_BannerSelection.Enabled = false;

                        // go over all the fields, (and update only those which haven't been modified by the user - will do that later)
                        foreach (String key in episode.FieldNames)
                        {
                            switch (key)
                            {
                                case DBEpisode.cSeasonIndex:
                                case DBEpisode.cEpisodeIndex:
                                case DBEpisode.cSeriesID:
                                case DBEpisode.cCompositeID:
                                case DBEpisode.cFilename:
                                case DBOnlineEpisode.cID:
                                    AddPropertyBindingSource(DBEpisode.PrettyFieldName(key), key, episode[key], false);
                                    break;

                                case DBEpisode.cEpisodeName:
                                    AddPropertyBindingSource(DBEpisode.PrettyFieldName(key), DBOnlineEpisode.cEpisodeName, episode[key]);
                                    break;

                                case DBOnlineEpisode.cEpisodeName:
                                case DBEpisode.cImportProcessed:
                                case DBOnlineEpisode.cOnlineDataImported:
                                    // hide those, they are handled internally
                                    break;

                                default:
                                    AddPropertyBindingSource(DBEpisode.PrettyFieldName(key), key, episode[key]);
                                    break;

                            }
                        }
                    }
                    break;

                #endregion
                //////////////////////////////////////////////////////////////////////////////

                //////////////////////////////////////////////////////////////////////////////
                #region When Season Nodes is Clicked

                case DBSeason.cTableName:
                    {
                        DBSeason season = (DBSeason)node.Tag;

                        comboBox_BannerSelection.Items.Clear();
                        // populate banner dropdown
                        foreach (String filename in season.BannerList)
                        {
                            BannerComboItem newItem = new BannerComboItem(Path.GetFileName(filename), filename);
                            comboBox_BannerSelection.Items.Add(newItem);
                        }
                        comboBox_BannerSelection.Enabled = true;

                        if (season.Banner != String.Empty)
                        {
                            try
                            {
                                this.pictureBox_Series.Image = Image.FromFile(season.Banner);
                            }
                            catch (Exception)
                            {
                            }
                            foreach (BannerComboItem comboItem in comboBox_BannerSelection.Items)
                                if (comboItem.sFullPath == season.Banner)
                                {
                                    comboBox_BannerSelection.SelectedItem = comboItem;
                                    break;
                                }
                        }

                        // go over all the fields, (and update only those which haven't been modified by the user - will do that later)
                        foreach (String key in season.FieldNames)
                        {
                            switch (key)
                            {
                                case DBSeason.cBannerFileNames:
                                case DBSeason.cCurrentBannerFileName:
                                case DBSeason.cHasLocalFiles:
                                case DBSeason.cHasLocalFilesTemp:
                                    // hide those, they are handled internally
                                    break;

                                default:
                                    AddPropertyBindingSource(DBSeason.PrettyFieldName(key), key, season[key], false);
                                    break;

                            }
                        }
                    }
                    break;
                #endregion

                //////////////////////////////////////////////////////////////////////////////
                #region When Series Nodes is Clicked

                case DBSeries.cTableName:
                    {
                        DBSeries series = (DBSeries)node.Tag;

                        comboBox_BannerSelection.Items.Clear();
                        // populate banner dropdown
                        foreach (String filename in series.BannerList)
                        {
                            BannerComboItem newItem = new BannerComboItem(Path.GetFileName(filename), filename);
                            comboBox_BannerSelection.Items.Add(newItem);
                        }
                        comboBox_BannerSelection.Enabled = true;

                        if (series.Banner != String.Empty)
                        {
                            try
                            {
                                this.pictureBox_Series.Image = Image.FromFile(series.Banner);
                            }
                            catch (System.Exception)
                            {
                            	
                            }
                            foreach (BannerComboItem comboItem in comboBox_BannerSelection.Items)
                                if (comboItem.sFullPath == series.Banner)
                                {
                                    comboBox_BannerSelection.SelectedItem = comboItem;
                                    break;
                                }
                        }

                        // go over all the fields, (and update only those which haven't been modified by the user - will do that later)
                        foreach (String key in series.FieldNames)
                        {
                            switch (key)
                            {
                                case DBOnlineSeries.cBannerFileNames:
                                case DBOnlineSeries.cBannersDownloaded:
                                case DBOnlineSeries.cCurrentBannerFileName:
                                case DBOnlineSeries.cHasLocalFiles:
                                case DBOnlineSeries.cHasLocalFilesTemp:
                                case DBOnlineSeries.cOnlineDataImported:
                                case DBSeries.cDuplicateLocalName:
                                    // hide those, they are handled internally
                                    break;

                                case DBSeries.cParsedName:
                                case DBSeries.cID:
                                    AddPropertyBindingSource(DBSeries.PrettyFieldName(key), key, series[key], false);
                                    break;

                                default:
                                    AddPropertyBindingSource(DBSeries.PrettyFieldName(key), key, series[key]);
                                    break;

                            }
                        }
                    }
                    break;

                #endregion
                //////////////////////////////////////////////////////////////////////////////

            }
            #endregion
            //////////////////////////////////////////////////////////////////////////////
        }

        private void AddPropertyBindingSource(string FieldPrettyName, string FieldName, string FieldValue)
        {
            AddPropertyBindingSource(FieldPrettyName, FieldName, FieldValue, true, DataGridViewContentAlignment.MiddleLeft);
        }

        private void AddPropertyBindingSource(string FieldPrettyName, string FieldName, string FieldValue, bool CanModify)
        {
            AddPropertyBindingSource(FieldPrettyName, FieldName, FieldValue, CanModify, DataGridViewContentAlignment.MiddleLeft);
        }

        private void AddPropertyBindingSource(string FieldPrettyName, string FieldName, string FieldValue, bool CanModify, DataGridViewContentAlignment TextAlign)
        {
            int id = this.detailsPropertyBindingSource.Add(new DetailsProperty(FieldPrettyName, FieldValue));

            DataGridViewCell cell = this.dataGridView1.Rows[id].Cells[0];
            cell.ReadOnly = true;

            cell = this.dataGridView1.Rows[id].Cells[1];
            cell.Tag = FieldName;
            if (!CanModify)
            {
                cell.ReadOnly = true;
                cell.Style.BackColor = System.Drawing.SystemColors.Control;
            }

            cell.Style.Alignment = TextAlign;
        }

        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            nodeEdited = treeView_Library.SelectedNode;
            /*
            if (this.dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString() == "Filename")
            {
                openFileDialog1.FileName = this.dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                openFileDialog1.ShowDialog();
                if (this.dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString() != openFileDialog1.FileName)
                {
                    this.dataGridView1.Rows[e.RowIndex].Cells[1].Value = openFileDialog1.FileName;
                    m_PropertySaveRequired = true;
                }
                e.Cancel = true;
                return;
            }

            if (this.treeView_Library.Nodes.Count > 0)
                m_PropertySaveRequired = true;
            */
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewCell cell = this.dataGridView1.Rows[e.RowIndex].Cells[1];
            if (nodeEdited != null)
            {
                switch (nodeEdited.Name)
                {
                    case DBSeries.cTableName:
                        DBSeries series = (DBSeries)nodeEdited.Tag;
                        series[(String)cell.Tag] = (String)cell.Value;
                        series.Commit();
                        if (series[DBOnlineSeries.cPrettyName] != String.Empty)
                            nodeEdited.Text = series[DBOnlineSeries.cPrettyName];
                        break;

                    case DBSeason.cTableName:
                        DBSeason season = (DBSeason)nodeEdited.Tag;
                        season[(String)cell.Tag] = (String)cell.Value;
                        season.Commit();
                        break;

                    case DBEpisode.cTableName:
                        DBEpisode episode = (DBEpisode)nodeEdited.Tag;
                        episode[(String)cell.Tag] = (String)cell.Value;
                        episode.Commit();
                        if (episode[DBEpisode.cEpisodeName] != String.Empty)
                            nodeEdited.Text = episode[DBEpisode.cSeasonIndex] + "x" + episode[DBEpisode.cEpisodeIndex] + " - " + episode[DBEpisode.cEpisodeName];
                        break;
                }
            }
        }
        #endregion

        #region UI actions

        private void treeView_Settings_AfterSelect(object sender, TreeViewEventArgs e)
        {
            foreach (Panel pane in m_paneList)
            {
                if (pane.Name == e.Node.Name)
                {
                    pane.Visible = true;
                }
                else
                    pane.Visible = false;
            }

            // special behavior for some nodes
            if (e.Node.Name == panel_ParsingTest.Name)
                TestParsing_Start(false);
        }

        private void button_Start_Click(object sender, EventArgs e)
        {
            Parsing_Start();
        }

        private void button_TestReparse_Click(object sender, EventArgs e)
        {
            TestParsing_Start(true);
        }
        #endregion

        private void checkBox_OnlineSearch_CheckedChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cOnlineParseEnabled, checkBox_OnlineSearch.Checked);
        }

        private void checkBox_FullSeriesRetrieval_CheckedChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cFullSeriesRetrieval, checkBox_FullSeriesRetrieval.Checked);
        }

        private void checkBox_AutoChooseSeries_CheckedChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cAutoChooseSeries, checkBox_AutoChooseSeries.Checked);
        }

        private void checkBox_LocalDataOverride_CheckedChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cLocalDataOverride, checkBox_LocalDataOverride.Checked);
        }

        private void comboBox_BannerSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (treeView_Library.SelectedNode.Name)
            {
                case DBSeries.cTableName:
                    {
                        DBSeries series = (DBSeries)treeView_Library.SelectedNode.Tag;
                        series.Banner = ((BannerComboItem)comboBox_BannerSelection.SelectedItem).sFullPath;
                        try
                        {
                            this.pictureBox_Series.Image = Image.FromFile(series.Banner);
                        }
                        catch (Exception)
                        {
                        }
                        series.Commit();
                    }
                    break;

                case DBSeason.cTableName:
                    {
                        DBSeason season = (DBSeason)treeView_Library.SelectedNode.Tag;
                        season.Banner = ((BannerComboItem)comboBox_BannerSelection.SelectedItem).sFullPath;
                        try
                        {
                            this.pictureBox_Series.Image = Image.FromFile(season.Banner);
                        }
                        catch (Exception)
                        {
                        }
                        season.Commit();
                    }
                    break;

                case DBEpisode.cTableName:
                    {
                        DBSeries series = (DBSeries)treeView_Library.SelectedNode.Parent.Parent.Tag;
                        series.Banner = ((BannerComboItem)comboBox_BannerSelection.SelectedItem).sFullPath;
                        try
                        {
                            this.pictureBox_Series.Image = Image.FromFile(series.Banner);
                        }
                        catch (Exception)
                        {
                        } 
                        series.Commit();
                    }
                    break;
            }
        }

        private void comboBox_BannerSelection_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        private void checkBox_Episode_MatchingLocalFile_CheckedChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cView_Episode_OnlyShowLocalFiles, checkBox_Episode_OnlyShowLocalFiles.Checked);
        }

        private void checkBox_Episode_HideUnwatchedSummary_CheckedChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cView_Episode_HideUnwatchedSummary, checkBox_Episode_HideUnwatchedSummary.Checked);
        }

        private void checkBox_AutoOnlineDataRefresh_CheckedChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cAutoUpdateOnlineData, checkBox_AutoOnlineDataRefresh.Checked);
            numericUpDown_AutoOnlineDataRefresh.Enabled = checkBox_AutoOnlineDataRefresh.Checked;
        }

        private void numericUpDown_AutoOnlineDataRefresh_ValueChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cAutoUpdateOnlineDataLapse, (int)numericUpDown_AutoOnlineDataRefresh.Value);
        }

        private void HideNode(TreeNode nodeHidden)
        {
            if (nodeHidden != null)
            {
                bool bHidden = false;
                switch (nodeHidden.Name)
                {
                    case DBSeries.cTableName:
                        DBSeries series = (DBSeries)nodeHidden.Tag;
                        series[DBSeries.cHidden] = !series[DBSeries.cHidden];
                        bHidden = series[DBSeries.cHidden];
                        series.Commit();
                        break;

                    case DBSeason.cTableName:
                        DBSeason season = (DBSeason)nodeHidden.Tag;
                        season[DBSeason.cHidden] = !season[DBSeason.cHidden];
                        bHidden = season[DBSeason.cHidden];
                        season.Commit();
                        break;

                    case DBEpisode.cTableName:
                        DBEpisode episode = (DBEpisode)nodeHidden.Tag;
                        episode[DBOnlineEpisode.cHidden] = !episode[DBOnlineEpisode.cHidden];
                        bHidden = episode[DBOnlineEpisode.cHidden];
                        episode.Commit();
                        break;
                }

                if (DBOption.GetOptions(DBOption.cShowHiddenItems))
                {
                    // change the font
                    if (bHidden)
                    {
                        Font fontDefault = treeView_Library.Font;
                        nodeHidden.NodeFont = new Font(fontDefault.Name, fontDefault.Size, FontStyle.Italic);
                    }
                    else
                    {
                        nodeHidden.NodeFont = treeView_Library.Font;
                    }
                }
                else
                {
                    // just remove the node
                    treeView_Library.Nodes.Remove(nodeHidden);
                }
            }
        }

        private void DeleteNode(TreeNode nodeDeleted)
        {
            if (nodeDeleted != null)
            {
                switch (nodeDeleted.Name)
                {
                    case DBSeries.cTableName:
                        if (MessageBox.Show("Are you sure you want to delete that series and all the underlying seasons and episodes?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            DBSeries series = (DBSeries)nodeDeleted.Tag;
                            SQLCondition condition = new SQLCondition();
                            condition.Add(new DBEpisode(), DBEpisode.cSeriesID, series[DBSeries.cID], SQLConditionType.Equal);
                            DBEpisode.Clear(condition);
                            condition = new SQLCondition();
                            condition.Add(new DBOnlineEpisode(), DBOnlineEpisode.cSeriesID, series[DBSeries.cID], SQLConditionType.Equal);
                            DBOnlineEpisode.Clear(condition);

                            condition = new SQLCondition();
                            condition.Add(new DBSeason(), DBSeason.cSeriesID, series[DBSeries.cID], SQLConditionType.Equal);
                            DBSeason.Clear(condition);

                            condition = new SQLCondition();
                            condition.Add(new DBSeries(), DBSeries.cID, series[DBSeries.cID], SQLConditionType.Equal);
                            DBSeries.Clear(condition);

                            condition = new SQLCondition();
                            condition.Add(new DBOnlineSeries(), DBOnlineSeries.cID, series[DBSeries.cID], SQLConditionType.Equal);
                            DBOnlineSeries.Clear(condition);

                            treeView_Library.Nodes.Remove(nodeDeleted);
                        }
                        break;

                    case DBSeason.cTableName:
                        if (MessageBox.Show("Are you sure you want to delete that season and all the underlying episodes?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            DBSeason season = (DBSeason)nodeDeleted.Tag;

                            SQLCondition condition = new SQLCondition();
                            condition.Add(new DBEpisode(), DBEpisode.cSeriesID, season[DBSeason.cSeriesID], SQLConditionType.Equal);
                            condition.Add(new DBEpisode(), DBEpisode.cSeasonIndex, season[DBSeason.cIndex], SQLConditionType.Equal);
                            DBEpisode.Clear(condition);
                            condition = new SQLCondition();
                            condition.Add(new DBOnlineEpisode(), DBOnlineEpisode.cSeriesID, season[DBSeason.cSeriesID], SQLConditionType.Equal);
                            condition.Add(new DBOnlineEpisode(), DBOnlineEpisode.cSeasonIndex, season[DBSeason.cIndex], SQLConditionType.Equal);
                            DBOnlineEpisode.Clear(condition);

                            condition = new SQLCondition();
                            condition.Add(new DBSeason(), DBSeason.cID, season[DBSeason.cID], SQLConditionType.Equal);
                            DBSeason.Clear(condition);

                            treeView_Library.Nodes.Remove(nodeDeleted);
                        }
                        break;

                    case DBEpisode.cTableName:
                        if (MessageBox.Show("Are you sure you want to delete that episode?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            DBEpisode episode = (DBEpisode)nodeDeleted.Tag;
                            SQLCondition condition = new SQLCondition();
                            condition.Add(new DBEpisode(), DBEpisode.cEpisodeName, episode[DBEpisode.cEpisodeName], SQLConditionType.Equal);
                            DBEpisode.Clear(condition);
                            condition = new SQLCondition();
                            condition.Add(new DBOnlineEpisode(), DBOnlineEpisode.cEpisodeName, episode[DBOnlineEpisode.cEpisodeName], SQLConditionType.Equal);
                            DBOnlineEpisode.Clear(condition);
                            treeView_Library.Nodes.Remove(nodeDeleted);
                        }
                        break;
                }
                if (treeView_Library.Nodes.Count == 0)
                {
                    // also clear the data pane
                    this.detailsPropertyBindingSource.Clear();
                    try
                    {
                        if (this.pictureBox_Series.Image != null)
                        {
                            this.pictureBox_Series.Image.Dispose();
                            this.pictureBox_Series.Image = null;
                        }
                    }
                    catch { }
                }
            }
        }

        private void treeView_Library_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteNode(treeView_Library.SelectedNode);
            }
        }

        private void FieldValidate(ref RichTextBox textBox)
        {
            FieldTag tag = textBox.Tag as FieldTag;
            if (!tag.m_bInited)
            {
                textBox.Text = DBOption.GetOptions(tag.m_sOptionName);
                tag.m_bInited = true;
            }

            int nCarret = textBox.SelectionStart;
            String s = textBox.Text;
            Color defColor = textBox.ForeColor;

            int nStart = 0;
            while (s.Length != 0)
            {
                int nTagStart = s.IndexOf('<');
                if (nTagStart != -1)
                {
                    String sCurrent = s.Substring(0, nTagStart);
                    s = s.Substring(nTagStart);

                    textBox.SelectionStart = nStart;
                    textBox.SelectionLength = sCurrent.Length;
                    textBox.SelectionColor = defColor;
                    nStart += sCurrent.Length;

                    int nTagEnd = s.IndexOf('>');
                    if (nTagEnd != -1)
                    {
                        sCurrent = s.Substring(0, nTagEnd + 1);
                        s = s.Substring(nTagEnd + 1);

                        bool bValid = false;
                        textBox.SelectionStart = nStart;
                        textBox.SelectionLength = sCurrent.Length;

                        // find out of the tag exists in the table(s)
                        String sTag = sCurrent.Substring(1, sCurrent.Length - 2);
                        if (sTag.IndexOf('.') != -1)
                        {
                            String sTableName = sTag.Substring(0, sTag.IndexOf('.'));
                            String sFieldName = sTag.Substring(sTag.IndexOf('.') + 1);

                            switch (tag.m_Level)
                            {
                                case FieldTag.Level.Series:
                                    if (sTableName == DBSeries.cOutName)
                                        bValid |= m_SeriesReference.FieldNames.Contains(sFieldName);
                                    break;

                                case FieldTag.Level.Season:
                                    if (sTableName == DBSeries.cOutName)
                                        bValid |= m_SeriesReference.FieldNames.Contains(sFieldName);
                                    if (sTableName == DBSeason.cOutName)
                                        bValid |= m_SeasonReference.FieldNames.Contains(sFieldName);
                                    break;

                                case FieldTag.Level.Episode:
                                    if (sTableName == DBSeries.cOutName)
                                        bValid |= m_SeriesReference.FieldNames.Contains(sFieldName);
                                    if (sTableName == DBSeason.cOutName)
                                        bValid |= m_SeasonReference.FieldNames.Contains(sFieldName);
                                    if (sTableName == DBEpisode.cOutName)
                                        bValid |= m_EpisodeReference.FieldNames.Contains(sFieldName);
                                    break;
                            }
                        }

                        if (bValid)
                            textBox.SelectionColor = Color.Green;
                        else
                            textBox.SelectionColor = Color.Red;
                        nStart += sCurrent.Length;

                    }
                    else
                    {
                        // no more closing tag, no good, red
                        textBox.SelectionStart = nStart;
                        textBox.SelectionLength = textBox.Text.Length - nStart;
                        textBox.SelectionColor = Color.Tomato;
                        s = String.Empty;
                    }
                }
                else
                {
                    // no more opening tag
                    textBox.SelectionStart = nStart;
                    textBox.SelectionLength = textBox.Text.Length - nStart;
                    textBox.SelectionColor = defColor;
                    s = String.Empty;
                }
            }

            textBox.SelectionLength = 0;
            textBox.SelectionStart = nCarret;

            DBOption.SetOptions(tag.m_sOptionName, textBox.Text);
        }

        private void richTextBox_TextChanged(object sender, EventArgs e)
        {
            RichTextBox textBox = sender as RichTextBox;
            FieldValidate(ref textBox);
        }

        private void contextMenuStrip_SeriesFields_Opening(object sender, CancelEventArgs e)
        {
            // Acquire references to the owning control and item.
            RichTextBox textBox = contextMenuStrip_InsertFields.SourceControl as RichTextBox;

            // Clear the ContextMenuStrip control's Items collection.
            contextMenuStrip_InsertFields.Items.Clear();
            contextMenuStrip_InsertFields.CanOverflow = true;

            contextMenuStrip_InsertFields.Items.Add("Add a field Value:");
            contextMenuStrip_InsertFields.Items[0].Enabled = false;
            // Populate the ContextMenuStrip control with its default items.
            contextMenuStrip_InsertFields.Items.Add("-");
            contextMenuStrip_InsertFields.Items[1].Enabled = false;

            FieldTag tag = textBox.Tag as FieldTag;

            // series' always there
            {
                ToolStripMenuItem subMenuItem = new ToolStripMenuItem(DBSeries.cOutName + " values");
                ContextMenuStrip subMenu = new ContextMenuStrip(this.components);
                subMenu.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
                subMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStrip_SeriesFields_ItemClicked);
                subMenuItem.DropDown = subMenu;
                List<String> fieldList = m_SeriesReference.FieldNames;
                fieldList.Remove(DBOnlineSeries.cHasLocalFiles);
                fieldList.Remove(DBOnlineSeries.cHasLocalFilesTemp);
                fieldList.Remove(DBOnlineSeries.cBannerFileNames);
                fieldList.Remove(DBOnlineSeries.cBannersDownloaded);
                fieldList.Remove(DBOnlineSeries.cCurrentBannerFileName);
                fieldList.Remove(DBOnlineSeries.cOnlineDataImported);
                fieldList.Remove(DBOnlineSeries.cGetEpisodesTimeStamp);
                fieldList.Remove(DBOnlineSeries.cUpdateBannersTimeStamp);
                fieldList.Remove(DBSeries.cScanIgnore);
                fieldList.Remove(DBSeries.cHidden);
                fieldList.Remove(DBSeries.cDuplicateLocalName);


                foreach (String sField in fieldList)
                {
                    ToolStripItem item = new ToolStripLabel();
                    item.Name = "<" + DBSeries.cOutName + "." + sField + ">";
                    item.Tag = textBox;
                    String sPretty = DBSeries.PrettyFieldName(sField);
                    if (sPretty == sField)
                        item.Text = item.Name;
                    else
                        item.Text = item.Name + " - (" + sPretty + ")";
                    subMenu.Items.Add(item);
                }
                contextMenuStrip_InsertFields.Items.Add(subMenuItem);
            }

            // season
            if (tag.m_Level == FieldTag.Level.Season || tag.m_Level == FieldTag.Level.Episode)
            {
                ToolStripMenuItem subMenuItem = new ToolStripMenuItem(DBSeason.cOutName + " values");
                ContextMenuStrip subMenu = new ContextMenuStrip(this.components);
                subMenu.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
                subMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStrip_SeriesFields_ItemClicked);
                subMenuItem.DropDown = subMenu;
                List<String> fieldList = m_SeasonReference.FieldNames;
                fieldList.Remove(DBSeason.cHasLocalFiles);
                fieldList.Remove(DBSeason.cHasLocalFilesTemp);
                fieldList.Remove(DBSeason.cHasEpisodes);
                fieldList.Remove(DBSeason.cHasEpisodesTemp);
                fieldList.Remove(DBSeason.cBannerFileNames);
                fieldList.Remove(DBSeason.cCurrentBannerFileName);
                fieldList.Remove(DBSeason.cHidden);
                foreach (String sField in fieldList)
                {
                    ToolStripItem item = new ToolStripLabel();
                    item.Name = "<" + DBSeason.cOutName + "." + sField + ">";
                    item.Tag = textBox;
                    String sPretty = DBSeason.PrettyFieldName(sField);
                    if (sPretty == sField)
                        item.Text = item.Name;
                    else
                        item.Text = item.Name + " - (" + sPretty + ")";
                    subMenu.Items.Add(item);
                }
                contextMenuStrip_InsertFields.Items.Add(subMenuItem);
            }

            // episode
            if (tag.m_Level == FieldTag.Level.Episode)
            {
                ToolStripMenuItem subMenuItem = new ToolStripMenuItem(DBEpisode.cOutName + " values");
                ContextMenuStrip subMenu = new ContextMenuStrip(this.components);
                subMenu.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
                subMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStrip_SeriesFields_ItemClicked);
                subMenuItem.DropDown = subMenu;
                List<String> fieldList = m_EpisodeReference.FieldNames;
                fieldList.Remove(DBEpisode.cImportProcessed);
                fieldList.Remove(DBOnlineEpisode.cOnlineDataImported);
                fieldList.Remove(DBOnlineEpisode.cHidden);
                fieldList.Remove(DBOnlineEpisode.cLastUpdated);

                foreach (String sField in fieldList)
                {
                    ToolStripItem item = new ToolStripLabel();
                    item.Name = "<" + DBEpisode.cOutName + "." + sField + ">";
                    item.Tag = textBox;
                    String sPretty = DBEpisode.PrettyFieldName(sField);
                    if (sPretty == sField)
                        item.Text = item.Name;
                    else
                        item.Text = item.Name + " - (" + sPretty + ")";
                    subMenu.Items.Add(item);
                }
                contextMenuStrip_InsertFields.Items.Add(subMenuItem);
            }

            // Set Cancel to false. 
            // It is optimized to true based on empty entry.
            e.Cancel = false;
        }

        private void contextMenuStrip_SeriesFields_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            // Acquire references to the owning control and item.
            RichTextBox textBox = e.ClickedItem.Tag as RichTextBox;
            if (textBox != null)
            {
                int nCarret = textBox.SelectionStart;
                textBox.Text = textBox.Text.Insert(textBox.SelectionStart, e.ClickedItem.Name);
                textBox.SelectionLength = 0;
                textBox.SelectionStart = nCarret;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.comboBox1.SelectedIndex == 0) MPTVSeriesLog.selectedLogLevel = MPTVSeriesLog.LogLevel.Normal;
            else if (this.comboBox1.SelectedIndex == 1) MPTVSeriesLog.selectedLogLevel = MPTVSeriesLog.LogLevel.Debug;
            else MPTVSeriesLog.selectedLogLevel = MPTVSeriesLog.LogLevel.Normal;
        }

        private void checkBox_AutoHeight_CheckedChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cViewAutoHeight, checkBox_AutoHeight.Checked);
        }

        private void comboBox_seasonFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cView_Season_ListFormat, comboBox_seasonFormat.SelectedIndex);

            richTextBox_seasonFormat_Col1.Enabled = (comboBox_seasonFormat.SelectedIndex == 0);
            richTextBox_seasonFormat_Col2.Enabled = (comboBox_seasonFormat.SelectedIndex == 0);
            richTextBox_seasonFormat_Col3.Enabled = (comboBox_seasonFormat.SelectedIndex == 0);
        }

        private void comboBox_seriesFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cView_Series_ListFormat, comboBox_seriesFormat.SelectedIndex);

            richTextBox_seriesFormat_Col1.Enabled = (comboBox_seriesFormat.SelectedIndex == 0);
            richTextBox_seriesFormat_Col2.Enabled = (comboBox_seriesFormat.SelectedIndex == 0);
            richTextBox_seriesFormat_Col3.Enabled = (comboBox_seriesFormat.SelectedIndex == 0);
        }

        private void textBox_PluginHomeName_TextChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cView_PluginName, textBox_PluginHomeName.Text);
        }

        private void textBox_foromID_TextChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cSubs_Forom_ID, textBox_foromID.Text);
        }

        private void textBox_foromBaseURL_TextChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cSubs_Forom_BaseURL, textBox_foromBaseURL.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
            DBOption.SetOptions(DBOption.cConfig_LogCollapsed, splitContainer1.Panel2Collapsed);
        }

        private void checkBox_DontClearMissingLocalFiles_CheckedChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cDontClearMissingLocalFiles, checkBox_DontClearMissingLocalFiles.Checked);
        }

        private void checkBox_ShowHidden_CheckedChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cShowHiddenItems, checkBox_ShowHidden.Checked);
            LoadTree();
        }

        private void contextMenuStrip_DetailsTree_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            TreeNode clickedNode = contextMenuStrip_DetailsTree.Tag as TreeNode;
            switch (e.ClickedItem.Tag.ToString())
            {
                case "hide":
                    HideNode(clickedNode);
                    break;

                case "delete":
                    DeleteNode(clickedNode);
                    break;

                case "subtitle":
                    GetSubtitles(clickedNode);
                    break;

                case "torrent":
                    TorrentFile(clickedNode);
                    break;
            }
        }

        public void TorrentFile(TreeNode node)
        {
            switch (node.Name)
            {
                case DBSeries.cTableName:
                    DBSeries series = (DBSeries)node.Tag;
                    break;

                case DBSeason.cTableName:
                    DBSeason season = (DBSeason)node.Tag;
                    break;

                case DBEpisode.cTableName:
                    DBEpisode episode = (DBEpisode)node.Tag;
                    Torrent.TorrentLoad torrentLoad = new Torrent.TorrentLoad(this);
                    torrentLoad.TorrentLoadCompleted += new WindowPlugins.GUITVSeries.Torrent.TorrentLoad.TorrentLoadCompletedHandler(torrentLoad_TorrentLoadCompleted);
                    torrentLoad.Search(episode);
                    break;
            }
        }

        void torrentLoad_TorrentLoadCompleted(bool bOK)
        {
            
        }

        public Feedback.ReturnCode ChooseFromSelection(Feedback.CDescriptor descriptor, out Feedback.CItem selected)
        {
            ChooseFromSelectionDialog userSelection = new ChooseFromSelectionDialog(descriptor);
            DialogResult result = userSelection.ShowDialog();
            selected = userSelection.SelectedItem;
            switch (result)
            {
                case DialogResult.OK:
                    return Feedback.ReturnCode.OK;


                case DialogResult.Ignore:
                    return Feedback.ReturnCode.Ignore;

                case DialogResult.Cancel:
                default:
                    return Feedback.ReturnCode.Cancel;
            }
        }

        public bool NoneFound()
        {
            MessageBox.Show("No subtitles were found for this file", "error");
            return true;
        }
        
        private void GetSubtitles(TreeNode node)
        {
            switch (node.Name)
            {
                case DBSeries.cTableName:
                    DBSeries series = (DBSeries)node.Tag;
                    break;

                case DBSeason.cTableName:
                    DBSeason season = (DBSeason)node.Tag;
                    break;

                case DBEpisode.cTableName:
                    DBEpisode episode = (DBEpisode)node.Tag;
                    Subtitles.Forom forom = new Subtitles.Forom(this);
                    forom.SubtitleRetrievalCompleted += new WindowPlugins.GUITVSeries.Subtitles.Forom.SubtitleRetrievalCompletedHandler(forom_SubtitleRetrievalCompleted);
                    forom.GetSubs(episode);
                    break;
            }
        }

        void forom_SubtitleRetrievalCompleted(bool bFound)
        {
        }

        private void contextMenuStrip_DetailsTree_Opening(object sender, CancelEventArgs e)
        {
            TreeNode node = contextMenuStrip_DetailsTree.Tag as TreeNode;
            bool bHidden = false;
            switch (node.Name)
            {
                case DBSeries.cTableName:
                    DBSeries series = (DBSeries)node.Tag;
                    bHidden = series[DBSeries.cHidden];
                    contextMenuStrip_DetailsTree.Items[2].Enabled = false;
                    break;

                case DBSeason.cTableName:
                    DBSeason season = (DBSeason)node.Tag;
                    bHidden = season[DBSeason.cHidden];
                    contextMenuStrip_DetailsTree.Items[2].Enabled = false;
                    break;

                case DBEpisode.cTableName:
                    DBEpisode episode = (DBEpisode)node.Tag;
                    bHidden = episode[DBOnlineEpisode.cHidden];
                    contextMenuStrip_DetailsTree.Items[2].Enabled = true;
                    break;
            }
            if (bHidden)
                contextMenuStrip_DetailsTree.Items[0].Text = "UnHide";
            else
                contextMenuStrip_DetailsTree.Items[0].Text = "Hide";
        }

        private void treeView_Library_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                contextMenuStrip_DetailsTree.Tag = e.Node;
        }

        private void textBox_uTorrentPath_TextChanged(object sender, EventArgs e)
        {
            String sPath = textBox_uTorrentPath.Text;
            if (System.IO.File.Exists(sPath))
                textBox_uTorrentPath.BackColor = System.Drawing.SystemColors.ControlLightLight;
            else
                textBox_uTorrentPath.BackColor = System.Drawing.Color.Tomato;
            DBOption.SetOptions(DBOption.cUTorrentPath, sPath);
        }

        private void button_uTorrentBrowse_Click(object sender, EventArgs e)
        {
            openFileDialog.FileName = DBOption.GetOptions(DBOption.cUTorrentPath);
            openFileDialog.Filter = "Executable files (*.exe)|";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                DBOption.SetOptions(DBOption.cUTorrentPath, openFileDialog.FileName);
                textBox_uTorrentPath.Text = openFileDialog.FileName;
            }
        }

        private void comboBox_TorrentPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_currentTorrentSearch = comboBox_TorrentPreset.SelectedItem as DBTorrentSearch;
            if (m_currentTorrentSearch != null)
            {
                textBox_TorrentSearchUrl.Text = m_currentTorrentSearch[DBTorrentSearch.cSearchUrl];
                textBox_TorrentSearchRegex.Text = m_currentTorrentSearch[DBTorrentSearch.cSearchRegex];
                textBox_TorrentDetailsUrl.Text = m_currentTorrentSearch[DBTorrentSearch.cDetailsUrl];
                textBox_TorrentDetailsRegex.Text = m_currentTorrentSearch[DBTorrentSearch.cDetailsRegex];
                DBOption.SetOptions(DBOption.cTorrentSearch, m_currentTorrentSearch[DBTorrentSearch.cID]);
            }
            else
            {
                textBox_TorrentSearchUrl.Text = String.Empty;
                textBox_TorrentSearchRegex.Text = String.Empty;
                textBox_TorrentDetailsUrl.Text = String.Empty;
                textBox_TorrentDetailsRegex.Text = String.Empty;
            }
        }

        private void comboBox_TorrentPreset_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    {
                        // check if this string exists in the list
                        object selObj = null;
                        foreach (object obj in comboBox_TorrentPreset.Items)
                            if (obj.ToString() == comboBox_TorrentPreset.Text)
                            {
                                selObj = obj;
                                break;
                            }

                        if (selObj == null)
                        {
                            // create a new item
                            DBTorrentSearch newSearch = new DBTorrentSearch(comboBox_TorrentPreset.Text);
                            newSearch.Commit();
                            comboBox_TorrentPreset.Items.Add(newSearch);
                        }

                        // select item
                        comboBox_TorrentPreset.SelectedItem = selObj;
                    }
                    break;

                case Keys.Delete:
                    // delete the selection
                    if (comboBox_TorrentPreset.DroppedDown)
                    {
                        // delete the selected item
                        if (MessageBox.Show("Really delete " + comboBox_TorrentPreset.SelectedItem + " ?", "Confirm") == DialogResult.OK)
                        {
                            SQLCondition condition = new SQLCondition();
                            condition.Add(new DBTorrentSearch(), DBTorrentSearch.cID, comboBox_TorrentPreset.SelectedItem.ToString(), SQLConditionType.Equal);
                            DBTorrentSearch.Clear(condition);
                            comboBox_TorrentPreset.Items.Remove(comboBox_TorrentPreset.SelectedItem);

                            if (comboBox_TorrentPreset.Items.Count > 0)
                                comboBox_TorrentPreset.SelectedIndex = 0;
                        }
                    }
                    break;
            }

        }

        private void textBox_TorrentUrl_TextChanged(object sender, EventArgs e)
        {
            m_currentTorrentSearch[DBTorrentSearch.cSearchUrl] = textBox_TorrentSearchUrl.Text;
            m_currentTorrentSearch.Commit();
        }

        private void textBox_TorrentRegex_TextChanged(object sender, EventArgs e)
        {
            m_currentTorrentSearch[DBTorrentSearch.cSearchRegex] = textBox_TorrentSearchRegex.Text;
            m_currentTorrentSearch.Commit();
        }

        private void textBox_TorrentDetailsUrl_TextChanged(object sender, EventArgs e)
        {
            m_currentTorrentSearch[DBTorrentSearch.cDetailsUrl] = textBox_TorrentDetailsUrl.Text;
            m_currentTorrentSearch.Commit();
        }

        private void textBox_TorrentDetailsRegex_TextChanged(object sender, EventArgs e)
        {
            m_currentTorrentSearch[DBTorrentSearch.cDetailsRegex] = textBox_TorrentDetailsRegex.Text;
            m_currentTorrentSearch.Commit();
        }

        private void checkBox_RandBanner_CheckedChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions(DBOption.cRandomBanner, checkBox_RandBanner.Checked);
        }

        private void minHDWidth_TextChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions("minHDWidth", minHDWidth.Text);
        }

        private void minHDHeight_TextChanged(object sender, EventArgs e)
        {
            DBOption.SetOptions("minHDHeight", minHDHeight.Text);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogResult result = MessageBox.Show("Force update of previously read-out files?\n(No for new files only)", "Ignore already read-out files?", MessageBoxButtons.YesNoCancel);
            SQLCondition cond = new SQLCondition();
            cond.Add(new DBEpisode(), DBEpisode.cFilename, "", SQLConditionType.NotEqual);
            List<DBEpisode> episodes = new List<DBEpisode>();
            if(result == DialogResult.Yes)
            {
                // get all the episodes
                episodes = DBEpisode.Get(cond, false);
            }
            else if(result == DialogResult.No)
            {
                // only get the episodes that dont have their resolutions read out already
                cond.Add(new DBEpisode(), "videoWidth", 1, SQLConditionType.LessThan); // lessthan here because it can be -1 etc. for no. of failed attempts
                cond.Add(new DBEpisode(), "videoHeight", 0, SQLConditionType.Equal);
                episodes = DBEpisode.Get(cond, false);
            }
            
            if (episodes.Count > 0)
            {
                MPTVSeriesLog.Write("Force update of Video Resolutions....(Please be patient!)");
                BackgroundWorker resReader = new BackgroundWorker();
                resReader.DoWork += new DoWorkEventHandler(asyncReadResolutions);
                resReader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(asyncReadResolutionsCompleted);
                resReader.RunWorkerAsync(episodes);
            }

        }

        void asyncReadResolutionsCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MPTVSeriesLog.Write("Force update of Video Resolutions complete (processed " + e.Result.ToString() + " files)");
        }

        void asyncReadResolutions(object sender, DoWorkEventArgs e)
        {
            List<DBEpisode> episodes = (List<DBEpisode>)e.Argument;
            foreach (DBEpisode ep in episodes)
                    ep.readVidResolution();
            e.Result = episodes.Count;
        }
    }

    public class BannerComboItem
    {
        public String sName = String.Empty;
        public String sFullPath;

        public BannerComboItem(String sName, String sFullPath)
        {
            this.sName = sName;
            this.sFullPath = sFullPath;
        }

        public override String ToString()
        {
            return sName;
        }

    };

    public class FieldTag
    {
        public String m_sOptionName;
        public Level m_Level;
        public bool m_bInited = false;

        public enum Level
        {
            Series,
            Season,
            Episode
        }

        public FieldTag(String optionName, Level level)
        {
            m_sOptionName = optionName;
            m_Level = level;
        }
    };

    public class DetailsProperty
    {
        String m_Property = String.Empty;
        String m_Value = String.Empty;

        public DetailsProperty(String property, String value)
        {
            this.m_Property = property;
            this.m_Value = value;
        }

        public String Property
        {
            get
            {
                return this.m_Property;
            }
            set
            {
                this.m_Property = value;
            }
        }
        public String Value
        {
            get
            {
                return this.m_Value;
            }
            set
            {
                this.m_Value = value;
            }
        }
    }
}

