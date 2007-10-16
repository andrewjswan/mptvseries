#region GNU license
// MP-TVSeries - Plugin for Mediaportal
// http://www.team-mediaportal.com
// Copyright (C) 2006-2007
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
#endregion


using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;

namespace WindowPlugins.GUITVSeries
{
    public class LocalParse
    {

        private BackgroundWorker worker = null;

        public delegate void LocalParseProgressHandler(int nProgress, List<parseResult> results);
        public delegate void LocalParseCompletedHandler(List<parseResult> results);
        public event LocalParseProgressHandler LocalParseProgress;
        public event LocalParseCompletedHandler LocalParseCompleted;

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
            List<String> listFolders = new List<string>();
            DBImportPath[] importPathes = DBImportPath.GetAll();
            if (importPathes != null)
            {
                foreach (DBImportPath importPath in importPathes)
                {
                    if (importPath[DBImportPath.cEnabled] != 0)
                    {
                        listFolders.Add(importPath[DBImportPath.cPath]);
                    }
                }
            }
            List<PathPair> files = Filelister.GetFiles(listFolders);
            e.Result = Parse(files);
        }

        static DBImportPath[] paths = null;

        public static bool isOnRemovable(string filename)
        {
            if (paths == null) paths = DBImportPath.GetAll();
            foreach (DBImportPath path in paths)
            {
                if (path[DBImportPath.cRemovable] && filename.ToLower().Contains(path[DBImportPath.cPath].ToString().ToLower())) return true;
            }
            return false;
        }

        public static string getDiskID(string filename)
        {
            System.IO.DriveInfo di = new System.IO.DriveInfo(filename);
            if (di != null) return di.VolumeLabel;
            return string.Empty;
        }

        public static List<parseResult> Parse(List<PathPair> files)
        {
            return Parse(files, true);
        }
        public static List<parseResult> Parse(List<PathPair> files, bool includeFailed)
        {
            MPTVSeriesLog.Write("ParseLocal starting, processing " + files.Count.ToString() + " files..." );
            List<parseResult> results = new List<parseResult>();
            parseResult progressReporter;
            int nFailed = 0;
            FilenameParser parser = null;
            ListViewItem item = null;
            paths = null;
            foreach (PathPair file in files)
            {
                parser = new FilenameParser(file.sMatch_FileName);
                try
                {
                    if (isOnRemovable(file.sFull_FileName))
                    {
                        parser.Matches.Add(DBEpisode.cIsOnRemovable, "1");
                        parser.Matches.Add(DBEpisode.cVolumeLabel, getDiskID(file.sFull_FileName));
                    }
                    else parser.Matches.Add(DBEpisode.cIsOnRemovable, "0");
                }
                catch (Exception)
                {
                    MPTVSeriesLog.Write("Warning: Could not add VolumenLabel/IsOnRemovable Property to episode - are you using these as a capture group?");
                }

                item = new ListViewItem(file.sMatch_FileName);
                item.UseItemStyleForSubItems = true;
                
                progressReporter = new parseResult();
                
                // make sure we have all the necessary data for a full match
                if (!parser.Matches.ContainsKey(DBEpisode.cSeasonIndex) ||
                    !parser.Matches.ContainsKey(DBEpisode.cEpisodeIndex))
                {
                    if (parser.Matches.ContainsKey(DBSeries.cParsedName) &&
                        parser.Matches.ContainsKey(DBOnlineEpisode.cFirstAired))
                    {
                        try{ System.DateTime.Parse(parser.Matches[DBOnlineEpisode.cFirstAired]);}
                        catch (System.FormatException)
                        {
                            nFailed++;
                            progressReporter.failedAirDate = true;
                            progressReporter.success = false;
                            progressReporter.exception = "Airdate not valid";
                        }
                    }
                    else
                    {
                        progressReporter.success = false;
                        progressReporter.exception = "Parsing failed for " + file;

                        nFailed++;
                    }

                }
                else
                {
                    // make sure episode & season are properly matched (numerical values)
                    try { Convert.ToInt32(parser.Matches[DBEpisode.cSeasonIndex]); }
                    catch (System.FormatException)
                    {
                        nFailed++;
                        progressReporter.failedSeason = true;
                        progressReporter.success = false;
                        progressReporter.exception += "Season not numerical ";
                    }
                    try { Convert.ToInt32(parser.Matches[DBEpisode.cEpisodeIndex]); }
                    catch (System.FormatException)
                    {
                        nFailed++;
                        progressReporter.failedEpisode = true;
                        progressReporter.success = false;
                        progressReporter.exception += "Episode not numerical ";
                    }
                }

                progressReporter.match_filename = file.sMatch_FileName;
                progressReporter.full_filename = file.sFull_FileName;
                progressReporter.parser = parser;
                if(includeFailed ||progressReporter.success)
                    results.Add(progressReporter);
            }
            MPTVSeriesLog.Write("ParseLocal finished..");
            return results;
        }

        public void AsyncFullParse()
        {
            MPTVSeriesLog.Write("Starting Local Parsing operation - Async: yes", MPTVSeriesLog.LogLevel.Debug);
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerAsync();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MPTVSeriesLog.Write("Finished Parsing operation - Async: True", MPTVSeriesLog.LogLevel.Debug);
            List<parseResult> results = (List<parseResult>)e.Result;
            if (LocalParseCompleted != null)
                LocalParseCompleted.Invoke(results);
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            List<parseResult> results = (List<parseResult>)e.UserState;
            if (LocalParseProgress != null)
                LocalParseProgress.Invoke(e.ProgressPercentage, results);
        }
    }

    public class parseResult : IComparable<parseResult>
    {
        public bool success = true;
        public bool failedSeason = false;
        public bool failedEpisode = false;
        public bool failedAirDate = false;
        public string exception;
        public FilenameParser parser;
        public string match_filename;
        public string full_filename;

        #region IComparable<parseResult> Members

        public int CompareTo(parseResult other)
        {
            return this.full_filename.CompareTo(other.full_filename);
        }

        #endregion
    }
}
