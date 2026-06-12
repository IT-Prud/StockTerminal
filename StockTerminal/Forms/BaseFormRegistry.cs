using System;
using System.Collections.Generic;
using System.Text;

namespace StockTerminal.Forms
{
    class BaseFormRegistry
    {
        private object AccessMutex = new object();

        // the following is for counting instances of the specific subclass of BaseForm
        private Dictionary<string, int> InstanceCounts = new Dictionary<string, int>(20);
        private Dictionary<string, BaseForm> Instances = new Dictionary<string, BaseForm>(20);
        private Dictionary<string, BaseForm> DocNames = new Dictionary<string, BaseForm>(20);
        private Dictionary<string, Dictionary<string, BaseForm>> FormTypes = new Dictionary<string, Dictionary<string, BaseForm>>(20);

        public BaseForm.FormListChangeDelegate FormListChange = null;

        public static string GetFormTypeName(BaseForm FormInstance)
        {
            if (FormInstance != null)
            {
                return GetFormTypeName(FormInstance.GetType());
            }
            return null;
        }

        public static string GetFormTypeName(Type FormType)
        {
            return FormType.ToString().Replace('.', '_');
        }

        public static string GetDefaultDocName(BaseForm FormInstance)
        {
            if (FormInstance != null)
            {
                return GetDefaultDocName(FormInstance.GetType());
            }
            return null;
        }

        public static string GetDefaultDocName(Type FormType)
        {
            string[] typeParts = FormType.ToString().Split('.');
            string docName = typeParts[typeParts.Length - 1];

            for (int i = 0, n = docName.Length - 1; i < n; i++)
            {
                char c = docName[i];
                if (c >= 97 && c <= 122)
                {
                    c = docName[i + 1];
                    if (c >= 65 && c <= 90)
                    {
                        docName = docName.Insert(i + 1, " ");
                        i += 3;
                        n++;
                    }
                }
            }
            return docName;
        }

        public int GetInstanceCount(Type FormType)
        {
            int Count = 0;

            lock (AccessMutex)
            {
                if (!InstanceCounts.TryGetValue(GetFormTypeName(FormType), out Count))
                {
                    Count = 0;
                }
            }

            return Count;
        }

        public BaseForm GetByInstanceId(string InstanceId)
        {
            BaseForm form = null;

            if (InstanceId != null)
            {
                lock (AccessMutex)
                {
                    Instances.TryGetValue(InstanceId, out form);
                }
            }

            return form;
        }

        public BaseForm GetByDocumentName(string DocumentName)
        {
            BaseForm form = null;

            if (DocumentName != null)
            {
                lock (AccessMutex)
                {
                    DocNames.TryGetValue(DocumentName, out form);
                }
            }

            return form;
        }

        public List<BaseForm> GetByFormType(Type FormType)
        {
            Dictionary<string, BaseForm> dict = null;
            List<BaseForm> list = null;

            if (FormType != null)
            {
                string formTypeName = GetFormTypeName(FormType);

                lock (AccessMutex)
                {
                    if (FormTypes.TryGetValue(formTypeName, out dict))
                    {
                        if (dict != null)
                        {
                            list = new List<BaseForm>(dict.Count);
                            foreach (BaseForm form in dict.Values)
                            {
                                list.Add(form);
                            }
                        }
                    }
                }
            }

            return list;
        }

        public string RegisterInstance(BaseForm FormInstance, string PreferredInstanceId)
        {
            if (FormInstance == null) return null;

            string instanceId = null;
            string formTypeName = GetFormTypeName(FormInstance);

            Dictionary<string, BaseForm> dict = null;
            int idx = 0;
            int idxMaxAttempt = 0;
            bool found = false;

            lock (AccessMutex)
            {
                if (PreferredInstanceId != null && !Instances.ContainsKey(PreferredInstanceId))
                {
                    instanceId = PreferredInstanceId;
                    found = true;
                }
                else
                {
                    idx = GetInstanceCount(FormInstance.GetType());
                    idxMaxAttempt = idx + 100;

                    for (; idx < idxMaxAttempt; idx++)
                    {
                        instanceId = formTypeName + "_" + idx.ToString();

                        if (!Instances.ContainsKey(instanceId))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found) // try add random idx to instance id
                    {
                        Random rnd = new Random(DateTime.Now.Millisecond);
                        for (idx = 1; idx <= 50; idx++)
                        {
                            instanceId = formTypeName + "_" + rnd.Next().ToString();

                            if (!Instances.ContainsKey(instanceId))
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                }

                Instances[instanceId] = FormInstance;

                if (!FormTypes.TryGetValue(formTypeName, out dict))
                {
                    dict = new Dictionary<string, BaseForm>(10);
                    FormTypes.Add(formTypeName, dict);
                }
                dict[instanceId] = FormInstance;

                int formTypeCount = 0;
                if (InstanceCounts.TryGetValue(formTypeName, out formTypeCount))
                {
                    InstanceCounts[formTypeName] = formTypeCount + 1;
                }
                else
                {
                    InstanceCounts.Add(formTypeName, 1);
                }

                // below is 50% slower and generate first chance exception
                //try { InstanceCounts[formTypeName]++; }
                //catch { InstanceCounts.Add(formTypeName, 1); }
            }

            return instanceId;
        }

        public void UnRegisterInstance(BaseForm FormInstance)
        {
            if (FormInstance == null) return;

            string formTypeName = GetFormTypeName(FormInstance);
            Dictionary<string, BaseForm> dict = null;

            int InstanceCount = 0;
            bool Removed = false;

            lock (AccessMutex)
            {
                InstanceCount = Instances.Count;

                try { Instances.Remove(FormInstance.InstanceId); }
                catch { }

                if (InstanceCount != Instances.Count) Removed = true;

                try { DocNames.Remove(FormInstance.DocumentName); }
                catch { }

                try { InstanceCounts[formTypeName]--; }
                catch { }

                if (FormTypes.TryGetValue(formTypeName, out dict))
                {
                    try
                    {
                        dict.Remove(FormInstance.InstanceId);
                    }
                    catch { }

                    if (dict.Count <= 0) FormTypes.Remove(formTypeName);
                }
            }

            if (Removed && FormListChange != null) FormListChange.Invoke();
        }

        public bool RegisterDocumentName(BaseForm FormInstance, string NewDocumentName, string NewDocumentNameEn, out string NewDocumentNameOut, out string NewDocumentNameEnOut)
        {
            bool nameChanged = false;

            NewDocumentNameOut = null;
            NewDocumentNameEnOut = null;
            if (FormInstance == null) return false;
            
            string newDocumentName = NewDocumentName != null ? NewDocumentName.Trim() : "";
            string newDocumentNameEn = NewDocumentNameEn != null ? NewDocumentNameEn.Trim() : "";

            string curDocumentName = FormInstance.DocumentName;

            if (curDocumentName == null || !curDocumentName.Equals(newDocumentName, StringComparison.CurrentCultureIgnoreCase))
            {
                nameChanged = true;

                string docTypeName = newDocumentName;
                string idxString = " ";
                BaseForm formInstance;

                bool found = false;

                lock (AccessMutex)
                {
                    if (curDocumentName != null && DocNames.TryGetValue(curDocumentName, out formInstance) && formInstance.Equals(FormInstance))
                    {
                        DocNames.Remove(curDocumentName);
                    }

                    if (newDocumentName.Length > 0 && !DocNames.ContainsKey(newDocumentName))
                    {
                        found = true;
                    }
                    else
                    {
                        int idx;

                        if (found == false) // try add sequential idx to document name
                        {
                            for (idx = 1; idx < 100; idx++)
                            {
                                idxString = " " + idx.ToString();
                                newDocumentName = docTypeName + idxString;
                                if (DocNames.ContainsKey(newDocumentName) == false)
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                        if (found == false) // try add random idx to document name
                        {
                            Random rnd = new Random(DateTime.Now.Millisecond);
                            for (idx = 1; idx < 50; idx++)
                            {
                                idxString = " " + rnd.Next().ToString();
                                newDocumentName = docTypeName + idxString;
                                if (DocNames.ContainsKey(newDocumentName) == false)
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (found == true)
                    {
                        DocNames.Add(newDocumentName, FormInstance);
                        newDocumentNameEn += idxString;
                    }
                    curDocumentName = newDocumentName;
                }
            }
            NewDocumentNameOut = curDocumentName;
            NewDocumentNameEnOut = newDocumentNameEn;

            return nameChanged;
        }
    }
}
