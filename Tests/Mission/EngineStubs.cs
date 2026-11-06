using System;
using System.Collections.Generic;
using Fusion;

// Runs the actual RoleManager.cs rules with lightweight engine stand-ins.
// Does not simulate networking, weaving, authority transfer, or Unity lifecycle.
namespace UnityEngine { public class HeaderAttribute : Attribute { public HeaderAttribute(string s) {} } }
namespace Fusion {
    public class NetworkedAttribute : Attribute {}
    public enum RpcSources { All, StateAuthority }
    public enum RpcTargets { All, StateAuthority }
    public class RpcAttribute : Attribute { public RpcAttribute(RpcSources s, RpcTargets t) {} }
    public struct PlayerRef : IEquatable<PlayerRef> {
        public int Id; public static PlayerRef None => new PlayerRef();
        public bool Equals(PlayerRef other) => Id == other.Id;
        public override bool Equals(object o) => o is PlayerRef p && Equals(p);
        public override int GetHashCode() => Id;
        public static bool operator ==(PlayerRef a, PlayerRef b) => a.Equals(b);
        public static bool operator !=(PlayerRef a, PlayerRef b) => !a.Equals(b);
    }
    public struct NetworkBool {
        private bool value;
        public static implicit operator bool(NetworkBool b) => b.value;
        public static implicit operator NetworkBool(bool b) => new NetworkBool { value = b };
    }
    public struct _64 {} public struct _32 {} public struct _128 {}
    public struct NetworkString<T> {
        private string value;
        public static implicit operator NetworkString<T>(string s) => new NetworkString<T> { value = s };
        public override string ToString() => value;
    }
    public struct RpcInfo { public PlayerRef Source; }
    public class NetworkObject { public bool IsValid = true; public PlayerRef StateAuthority; public UnityEngine.Transform transform=new UnityEngine.Transform(); }
    public class NetworkRunner { public double SimulationTime=1; public PlayerRef LocalPlayer; public List<PlayerRef> ActivePlayers=new List<PlayerRef>(); public Dictionary<PlayerRef,NetworkObject> Avatars=new Dictionary<PlayerRef,NetworkObject>(); public NetworkObject GetPlayerObject(PlayerRef p)=>Avatars.TryGetValue(p,out var a)?a:null; }
    public class NetworkBehaviour : UnityEngine.Object {
        public NetworkRunner Runner; public NetworkObject Object = new NetworkObject(); public bool HasStateAuthority = true;
        public virtual void Render() {} public virtual void Spawned() {} public virtual void Despawned(NetworkRunner r, bool s) {} public virtual void FixedUpdateNetwork() {}
    }
}
namespace AvocadoShark {
    public class FusionConnection {
        public static FusionConnection Instance;
        public bool TryGetLocalPlayerComponent<T>(out T value) { value = default; return false; }
    }
}
public class PlayerRole { public void SetRole(RoleType role) {} }
namespace UnityEngine {
 public class Object { public static readonly Dictionary<Type,Array> Registry=new Dictionary<Type,Array>(); public static T[] FindObjectsByType<T>(FindObjectsSortMode mode)=>Registry.TryGetValue(typeof(T),out var a)?(T[])a:Array.Empty<T>(); }
 public enum FindObjectsSortMode{None}
 public class ScriptableObject : Object {}
 public class SerializeField : Attribute {}
 public class CreateAssetMenuAttribute:Attribute{public string menuName;public string fileName;}
 public class MinAttribute:Attribute{public MinAttribute(float a){}}
 public class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}
 public class TextAreaAttribute:Attribute{public TextAreaAttribute(int a,int b){}}
 public class TooltipAttribute:Attribute{public TooltipAttribute(string a){}}
 public class Transform{public Vector3 position;}
 public struct Vector3{public float x,y,z; public static float Distance(Vector3 a,Vector3 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));}
 public static class Mathf{public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Clamp01(float v)=>Math.Clamp(v,0,1);}
 public static class Debug{public static void Log(object o){}}
}
namespace Fusion {
 public interface INetworkStruct{}
 public class CapacityAttribute:Attribute{public CapacityAttribute(int n){}}
 public struct NetworkArray<T> { private static T[] data=new T[typeof(T)==typeof(MissionRecord)?MissionNetworkBudget.EventCapacity:MissionNetworkBudget.TicketCapacity]; public int Length=>data.Length; public T this[int i]=>data[i];public void Set(int i,T v){data[i]=v;} }
 public struct TickTimer{public double Until;public static TickTimer None=>default;public bool ExpiredOrNotRunning(NetworkRunner r)=>Until<=r.SimulationTime;public bool Expired(NetworkRunner r)=>Until>0 && Until<=r.SimulationTime;public float? RemainingTime(NetworkRunner r)=>Until==0?null:(float)Math.Max(0,Until-r.SimulationTime);public static TickTimer CreateFromSeconds(NetworkRunner r,float s)=>new TickTimer{Until=r.SimulationTime+s};}
}
public class ServidorRed{public UnityEngine.Transform transform=new UnityEngine.Transform();}
public class ConsolaControl{public UnityEngine.Transform transform=new UnityEngine.Transform();public ServidorRed servidorAsociado;}
