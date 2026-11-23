using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.InputSystem;
namespace Unity.AI.Assistant.PlayModeTest
{
 [InitializeOnLoad] internal static class PlayModeTestRunner
 {
  static MonoBehaviour flow, sequence, actor; static Camera camera; static Transform player;
  static int phase; static float started, analysisStarted; static bool captured; static string failure;
  static List<string> logs=new List<string>(); static int frames;
  static BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  static PlayModeTestRunner()
  {
   var state=SessionState.GetString("PlayModeTest.State","Idle");
   if(state=="WaitingForCompile") EditorApplication.delayCall+=()=>{SessionState.SetString("PlayModeTest.State","EnteringPlayMode");EditorApplication.playModeStateChanged+=Changed;EditorApplication.isPlaying=true;};
   else if(state=="EnteringPlayMode") {EditorApplication.playModeStateChanged+=Changed;if(EditorApplication.isPlaying) Begin();}
   else if(state=="InPlayMode" && EditorApplication.isPlaying) Begin();
  }
  static void Changed(PlayModeStateChange change){if(change==PlayModeStateChange.EnteredPlayMode)Begin();}
  static void Begin(){EditorApplication.playModeStateChanged-=Changed;SessionState.SetString("PlayModeTest.State","InPlayMode");EditorApplication.update+=Tick;}
  static object Field(object o,string name){return o.GetType().GetField(name,flags).GetValue(o);}
  static object Property(object o,string name){return o.GetType().GetProperty(name,flags).GetValue(o);}
  static void Set(object o,string name,object value){o.GetType().GetField(name,flags).SetValue(o,value);}
  static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,flags).Invoke(o,args);}
  static string Stage(){return Property(flow,"Stage").ToString();}
  static void Log(string text){logs.Add(text);Debug.Log("[Test] "+text);}
  static void Require(bool value,string text){if(!value)throw new Exception(text);Log(text);}
  static void Key(bool down)
  {
   var keyboard=UnityEngine.InputSystem.Keyboard.current;
   using(var state=UnityEngine.InputSystem.LowLevel.StateEvent.From(keyboard,out var ptr))
   {keyboard.eKey.WriteValueIntoEvent(down?1f:0f,ptr);UnityEngine.InputSystem.InputSystem.QueueEvent(ptr);}
  }
  static void Aim(Vector3 centre,Vector3 outward,float distance)
  {
   var pos=centre+outward*distance;player.position=pos-Vector3.up*2f;
   camera.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(centre-pos,Vector3.up));Physics.SyncTransforms();
  }
  static void Setup()
  {
   var objects=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   flow=objects.Select(t=>t.GetComponent("AAH_RoomTreatmentSequence") as MonoBehaviour).First(c=>c!=null);
   sequence=(MonoBehaviour)Field(flow,"_checkIn");camera=(Camera)Field(flow,"_playerCamera");player=(Transform)Field(flow,"_player");
   var controller=player.GetComponent("SC_FPSController") as Behaviour;if(controller!=null)controller.enabled=false;
   var cc=player.GetComponent<CharacterController>();if(cc!=null)cc.enabled=false;
   var arrival=sequence.GetComponent("AAH_PatientArrivalController") as MonoBehaviour;arrival.StopAllCoroutines();
   var profiles=(Array)Field(arrival,"_patients");var profile=profiles.GetValue(0);
   var registry=Field(sequence,"roomRegistry");var rooms=(Array)Property(registry,"Rooms");var room=rooms.GetValue(0);
   var approach=(Transform)Field(room,"bedApproach");
   UnityEngine.AI.NavMeshHit hit;Require(UnityEngine.AI.NavMesh.SamplePosition(approach.position,out hit,2f,UnityEngine.AI.NavMesh.AllAreas),"Bed approach lies on NavMesh");
   var go=new GameObject("TreatmentTest_PAT001");go.SetActive(false);go.transform.position=hit.position;
   var nav=go.AddComponent<UnityEngine.AI.NavMeshAgent>();nav.agentTypeID=(int)Field(arrival,"_agentTypeId");nav.radius=1f;nav.height=5.4f;nav.speed=6f;nav.stoppingDistance=0.15f;
   actor=go.AddComponent(profiles.GetType().GetElementType().DeclaringType) as MonoBehaviour;
   Call(actor,"Initialize",profile,"PAT-001",registry,Field(arrival,"_doors"));go.SetActive(true);
   Require((bool)Call(sequence,"BeginPatient",actor),"Existing reception reserves room 01 for fixture patient");
   // Use the actual check-in event while retaining the real patient visual and bed pose.
   var callback=Field(sequence,"OnPatientCheckedIn") as Delegate;callback.DynamicInvoke(Property(actor,"DisplayName"),1);
   var shortRoute=Activator.CreateInstance(room.GetType());
   foreach(var field in room.GetType().GetFields())field.SetValue(shortRoute,field.GetValue(room));
   Set(shortRoute,"entry",approach);Call(actor,"LeaveForRoom",shortRoute);
   Require(Stage()=="WaitingForBed","Treatment locked until lying on bed");
   started=Time.time;
  }
  static void Capture(string path)
  {
   Log("Capture camera pos="+camera.transform.position+" forward="+camera.transform.forward);
   var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var rt=RenderTexture.GetTemporary(1024,768,24);
   camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
   var texture=new Texture2D(1024,768,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1024,768),0,0);texture.Apply();
   System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
   camera.targetTexture=oldTarget;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(rt);
   SessionState.SetString("PlayModeTest.ScreenshotPath",path);Log("Screenshot "+path);
  }
  static void Tick()
  {
   try
   {
    if(++frames<8)return;
    if(flow==null){Setup();return;}
    if(Time.time-started>25f)throw new Exception("Treatment fixture timed out in "+Stage());
    if(phase==0)
    {
     if(Stage()!="Patient")return;
     Require(Property(actor,"State").ToString()=="AtBed","Patient AtBed activates examination");
     var computer=(Renderer)Field(flow,"_computer");Aim(computer.bounds.center,Vector3.left,2.5f);Key(true);phase=1;return;
    }
    if(phase==1){Key(false);Require(Stage()=="Patient","Computer E rejected before patient examination");var b=(Bounds)Property(actor,"VisualBounds");Aim(b.center,Vector3.up,3f);phase=2;return;}
    if(phase==2){Require((bool)Call(flow,"SelectTarget"),"Hover selects lying patient's fitted collider");Key(true);phase=3;return;}
    if(phase==3){Key(false);Require(Stage()=="Scan","E patient -> scan");var scan=(Renderer)Field(flow,"_scan");Aim(scan.bounds.center,Vector3.left,2.5f);phase=4;return;}
    if(phase==4){Require((bool)Call(flow,"SelectTarget"),"Hover selects scan_01");Key(true);phase=5;return;}
    if(phase==5){Key(false);Require(Stage()=="Computer","E scan -> computer");var screen=(Collider)Field(flow,"_computerScreenCollider");Aim(screen.bounds.center,Vector3.left,2.5f);phase=6;return;}
    if(phase==6){Require((bool)Call(flow,"SelectTarget"),"Hover selects computer screen as computer_01");Key(true);phase=7;return;}
    if(phase==7){Key(false);Require(Stage()=="Analyzing","E computer starts analysis");analysisStarted=Time.time;phase=8;return;}
    if(phase==8)
    {
     var progress=(float)Property(flow,"Progress");
     if(!captured && progress>=0.45f){Capture("Assets/Room01_ProgressTest.png");captured=true;Log("Mid-progress="+progress);}
     if(Stage()!="Completed")return;
     Require(Mathf.Abs((Time.time-analysisStarted)-5f)<0.3f,"Analysis completes in 5s; actual="+(Time.time-analysisStarted));
     Require(progress==1f,"Progress reaches 100%");
     var fill=(UnityEngine.UI.Image)Field(flow,"_progressFill");Require(fill.fillAmount==1f && fill.color.b>0.9f,"Sky-blue filled bar reaches 100%");
     var meds=(TMPro.TMP_Text)Field(flow,"_medicineText");Require(meds.text.Contains("Mai Yamada") && meds.text.Contains("Tylenol") && meds.text.Contains("Eye drops"),"TV contains correct patient and configured medicine names");
     var tv=GameObject.Find("TV01_MedicineDisplay");Aim(tv.transform.position,Vector3.back,6f);phase=9;return;
    }
    if(phase==9){Capture("Assets/Room01_TVTest.png");phase=10;return;}
    if(phase==10){Finish(true,null);}
   }
   catch(Exception e){Finish(false,e.ToString());}
  }
  [Serializable] class Result{public bool success;public string error;public string[] logs;}
  static void Finish(bool success,string error)
  {
   EditorApplication.update-=Tick;
   SessionState.SetString("PlayModeTest.Result",JsonUtility.ToJson(new Result{success=success,error=error,logs=logs.ToArray()}));
   SessionState.SetString("PlayModeTest.State","Done");EditorApplication.isPlaying=false;
  }
 }
}
