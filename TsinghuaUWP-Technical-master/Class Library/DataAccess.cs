﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage;

namespace TsinghuaUWP {
    // Data Access Object
    // retrieve from memory/ local settings/ remote

    static public class DataAccess {
        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static List<Course> courses = null;
        private static List<Deadline> deadlines = null;
        private static List<Announce> announces = null;
        private static Semesters semesters = null;
        static string DEADLINES_FILENAME = "deadlines.json";
        static string SEMESTERS_FILENAME = "semesters.json";
        static string COURSES_FILENAME = "courses.json";
        static string ANNOUNCE_FILENAME = "announce.json";

        static public Windows.Foundation.Collections.IPropertySet getLocalSettings() {
            return localSettings.Values;
        }

        static async Task writeCache(string filename, string value) {
            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile file;
            try {
                file = await localCacheFolder.GetFileAsync(filename);
            } catch {
                file = await localCacheFolder.CreateFileAsync(filename);
            }
            await FileIO.WriteTextAsync(file, value);
        }

        static async Task<string> readCache(string filename) {
            try {
                StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
                StorageFile file = await localCacheFolder.GetFileAsync(filename);
                String fileContent = await FileIO.ReadTextAsync(file);
                return fileContent;
            }catch {
                return "";
            }
        }

        static public bool credentialAbsent() {
            var username = localSettings.Values["username"];
            return username == null
                || username.ToString() == "__anonymous";
        }

        static public bool supposedToWorkAnonymously() {
            var username = localSettings.Values["username"];
            return username != null
                && username.ToString() == "__anonymous";
        }

        static public bool isDemo() {
            return
                localSettings.Values["username"] != null &&
                localSettings.Values["username"].ToString() == "233333";
        }

        static public void setLocalSettings<Type>(string key, Type value) {
            localSettings.Values[key] = value;
        }

        static async public Task<int> updateAllFromRemote() {
            await getSemester(true);
            await getCourses(true);
            await getTimetable(true);
            await getAllDeadlines(true);
            await getAllAnnounce(true);
            return 0;
        }

        static public List<Deadline> sortDeadlines(List<Deadline> assignments, int limit = -1) {

            var future = (from assignment in assignments
                          where !assignment.isPast()
                          orderby assignment.daysFromNow() ascending
                          select assignment);

            int futureCount = future.Count();

            if (futureCount > limit && limit >= 0) {
                return future.Take(limit).ToList();
            }


            var past = (from assignment in assignments
                        where assignment.isPast()
                        orderby assignment.daysFromNow() descending
                        select assignment);


            if (limit < 0) {
                return future.Concat(past).ToList();
            }

            return future.Concat(past.Take(limit - futureCount)).ToList();
        }

        public static async Task<List<Course>> getCourses(bool forceRemote = false) {
            if (isDemo()) {
                var list = new List<Course>();
                list.Add(new Course {
                    name = "数据结构",
                    id = "demo_course_0",
                });

                list.Add(new Course {
                    name = "操作系统",
                    id = "demo_course_1",
                });

                return list;
            }

            if (!forceRemote) {
                //try memory
                if (courses != null) {
                    Debug.WriteLine("[getCourses] Returning memory");
                    return courses;
                }

                //try localSettings
                var localCourses = await readCache(COURSES_FILENAME);
                if (localCourses.Length > 0) {
                    Debug.WriteLine("[getCourses] Returning local settings");
                    courses = JSON.parse<List<Course>>(localCourses);
                    return courses;
                }
            }


            //fetch from remote
            var _courses = await Remote.getRemoteCourseList();
            courses = _courses;
            writeCache(COURSES_FILENAME, JSON.stringify(_courses));

            Debug.WriteLine("[getCourses] Returning remote");
            return courses;
        }

        public static async Task<Timetable> getTimetable(bool forceRemote = false) {
            if (isDemo()) {
                var table = new Timetable();

                var start = DateTime.Now.AddDays(-20);
                while (start.DayOfWeek != DayOfWeek.Monday)
                    start = start.AddDays(-1);

                for (var i = 0; i < 10; i++) {
                    table.Add(new Event {
                        nr = "形式语言与自动机",
                        dd = "六教 6A301",
                        nq = start.AddDays(i * 7 + 2).ToString("yyyy-MM-dd"),
                        kssj = "08:00",
                        jssj = "09:35"
                        
                    });

                    table.Add(new Event {
                        nr = "高级数据结构",
                        dd = "六教 6A301",
                        nq = start.AddDays(i * 7 + 2).ToString("yyyy-MM-dd"),
                        kssj = "09:50",
                        jssj = "11:25"
                    });

                    table.Add(new Event {
                        nr = "操作系统",
                        dd = "六教 6A303",
                        nq = start.AddDays(i * 7 + 3).ToString("yyyy-MM-dd"),
                        kssj = "09:50",
                        jssj = "11:25"
                    });

                    table.Add(new Event {
                        nr = "概率论与数理统计",
                        dd = "六教 6C102",
                        nq = start.AddDays(i * 7 + 4).ToString("yyyy-MM-dd"),
                        kssj = "15:20",
                        jssj = "16:55"
                    });

                    table.Add(new Event {
                        nr = "概率论与数理统计",
                        dd = "一教 104",
                        nq = start.AddDays(i * 7 + 1).ToString("yyyy-MM-dd"),
                        kssj = "13:30",
                        jssj = "15:05"
                    });
                }
                return table;
            }

            //fetch from remote
            var _remoteTimetable = await Remote.getRemoteTimetable();
            Debug.WriteLine("[getTimetable] Returning remote");
            return _remoteTimetable;
        }

        public static async Task<Semester> getSemester(bool forceRemote = false, bool getNextSemester = false) {
            if (isDemo()) {
                var start = DateTime.Now.AddDays(-20);
                while (start.DayOfWeek != DayOfWeek.Monday)
                    start = start.AddDays(-1);

                if(!getNextSemester)
                    return new Semester {
                        startDate = start.ToString("yyyy-MM-dd"),
                        endDate = start.AddDays(10 * 7 - 1).ToString("yyyy-MM-dd"),
                        semesterEname = "2333-2334-Spring",
                    };
                else
                    return new Semester {
                        startDate = start.AddDays(10 * 7).ToString("yyyy-MM-dd"),
                        endDate = start.AddDays(20 * 7 - 1).ToString("yyyy-MM-dd"),
                        semesterEname = "2334-2335-Autumn",
                    };
            }
            if (!forceRemote) {
                Semesters __semesters = null;
                //try memory
                if (semesters != null) {
                    Debug.WriteLine("[getCalendar] memory");
                    __semesters = semesters;
                } else //try localSettings
                  {
                    var localJSON = await readCache(SEMESTERS_FILENAME);
                    if (localJSON.Length > 0) {
                        Debug.WriteLine("[getCalendar] local settings");
                        __semesters = JSON.parse<Semesters>(localJSON);
                    }
                }

                //cache hit
                if (__semesters != null) {
                    if(getNextSemester)
                        return __semesters.nextSemester;

                    if (DateTime.Parse(__semesters.currentSemester.endDate + " 23:59") < DateTime.Now) {
                        //perform a remote update
                        Task task = getSemester(forceRemote: true);
                        task.ContinueWith((_) => 0);

                        Debug.WriteLine("[getCalendar] Returning cache next");
                        if (__semesters.nextSemester.endDate == null) {
                            //automatically complete missing endDate
                            if (__semesters.nextSemester.semesterEname.IndexOf("Autumn") != -1
                                || __semesters.nextSemester.semesterEname.IndexOf("Spring") != -1)
                                __semesters.nextSemester.endDate = DateTime.Parse(__semesters.nextSemester.startDate).AddDays(18 * 7 - 1).ToString();
                        }
                        return __semesters.nextSemester;
                    }
                    Debug.WriteLine("[getCalendar] Returning cache");
                    return __semesters.currentSemester;
                }
            }

            //fetch from remote
            Semesters _remoteSemesters = null;

            if (credentialAbsent() == false) {
                try {
                    _remoteSemesters = await Remote.getRemoteSemesters();
                } catch (Exception) { }
            }
            if (_remoteSemesters == null) {
                Debug.WriteLine("[getCalendar] remote fail, falling back to hosted");
                _remoteSemesters = await Remote.getHostedSemesters();
            }

            semesters = _remoteSemesters;
            writeCache(SEMESTERS_FILENAME, JSON.stringify(semesters));

            Debug.WriteLine("[getCalendar] Returning remote");
            return semesters.currentSemester;
        }

        static public async Task<List<Deadline>> getAllDeadlines(bool forceRemote = false) {
            if (isDemo()) {
                var list = new List<Deadline>();
                var start = DateTime.Now.AddDays(-20);
                while (start.DayOfWeek != DayOfWeek.Monday)
                    start = start.AddDays(-1);

                for (var i = 0; i <= 3; i++) {
                    list.Add(new Deadline {
                        course = "操作系统",
                        ddl = start.AddDays(i * 7 + 4 + 7).ToString("yyyy-MM-dd"),
                        name = $"代码阅读报告{i + 1}",
                        hasBeenFinished = (i < 3),
                        id = "operating_systems_" + i.ToString(),
                        courseid="demo_course_1"
                    });
                }

                for (var i = 0; i <= 3; i++) {
                    list.Add(new Deadline {
                        course = "数据结构",
                        ddl = start.AddDays(i * 7 + 3 + 7).ToString("yyyy-MM-dd"),
                        name = $"数据结构习题{i + 1}",
                        hasBeenFinished = (i < 3),
                        id = "data_structure_" + i.ToString(),
                        courseid = "demo_course_0"
                    });
                }

                return list;
            }
            if (!forceRemote) {
                //try session memory
                if (deadlines != null) {
                    Debug.WriteLine("[getAllDeadlines] Returning memory");
                    return deadlines;
                }


                //try local
                var cache = await readCache(DEADLINES_FILENAME);
                if (cache.Length > 0) {
                    Debug.WriteLine("[getAllDeadlines] Returning local");
                    return JSON.parse<List<Deadline>>(cache);
                }
            }

            //fetch from remote

            List<Deadline> _deadlines = new List<Deadline>();

            foreach (var course in await getCourses(forceRemote)) {
                Debug.WriteLine("[getAllDeadlines] Remote " + course.name);
                var id = course.id;
                List<Deadline> __deadlines;
                if (course.isNew)
                    __deadlines = await Remote.getRemoteHomeworkListNew(id);
                else
                    __deadlines = await Remote.getRemoteHomeworkList(id);
                _deadlines = _deadlines.Concat(__deadlines).ToList();
            }


            deadlines = _deadlines;
            writeCache(DEADLINES_FILENAME, JSON.stringify(deadlines));
            
            Debug.WriteLine("[getAllDeadlines] Returning remote");

            return _deadlines;
        }
        static public async Task<List<Announce>> getAllAnnounce(bool forceRemote = false)
        {
            if (isDemo())
            {
                var list = new List<Announce>();
                var start = DateTime.Now.AddDays(-20);
                while (start.DayOfWeek != DayOfWeek.Monday)
                    start = start.AddDays(-1);

                for (var i = 0; i <= 3; i++)
                {
                    list.Add(new Announce
                    {
                        owner = "Trump",
                        regDate = start.AddDays(i * 7 + 4 + 7).ToString("yyyy-MM-dd"),
                        title = $"代码阅读报告{i + 1}",
                        id = "operating_systems_" + i.ToString(),
                        courseId = "demo_course_1"
                    });
                }

                for (var i = 0; i <= 3; i++)
                {
                    list.Add(new Announce
                    {
                        owner = "邓俊辉",
                        regDate = start.AddDays(i * 7 + 3 + 7).ToString("yyyy-MM-dd"),
                        title = $"数据结构习题{i + 1}",
                        id = "data_structure_" + i.ToString(),
                        courseId = "demo_course_0"
                    });
                }

                return list;
            }
            if (!forceRemote)
            {
                //try session memory
                if (announces != null)
                {
                    Debug.WriteLine("[getAllAnnounce] Returning memory");
                    return announces;
                }


                //try local
                var cache = await readCache(ANNOUNCE_FILENAME);
                if (cache.Length > 0)
                {
                    Debug.WriteLine("[getAllAnnounce] Returning local");
                    return JSON.parse<List<Announce>>(cache);
                }
            }

            //fetch from remote

            List<Announce> _announces = new List<Announce>();

            foreach (var course in await getCourses(forceRemote))
            {
                Debug.WriteLine("[getAllAnnounces] Remote " + course.name);
                var id = course.id;
                List<Announce> __announces;
                if (course.isNew)
                    __announces = await Remote.getRemoteAncListNew(id);
                else
                    __announces = await Remote.getRemoteAncList(id);
                _announces = _announces.Concat(__announces).ToList();
            }


            announces = _announces;
            writeCache(ANNOUNCE_FILENAME, JSON.stringify(announces));

            Debug.WriteLine("[getAllAnnounces] Returning remote");

            return _announces;
        }




    }
}
