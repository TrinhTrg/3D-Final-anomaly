using System;
using UnityEngine;

public class AAH_RoomRegistry : MonoBehaviour
{
    [Serializable]
    public class RoomSlot
    {
        public int roomNumber;
        public Transform entry;
        public Transform bedApproach;
        public Transform bedPose;
        public Renderer bedRenderer;
        public AAH_PatientDoorAccess door;
        [NonSerialized] public string owner;
        [NonSerialized] public bool occupied;
    }
    [SerializeField] private RoomSlot[] _rooms = new RoomSlot[6];
    public RoomSlot[] Rooms => _rooms;
    public int FreeCount { get { int count = 0; foreach (var r in _rooms) if (r != null && string.IsNullOrEmpty(r.owner)) count++; return count; } }
    public event Action RoomsChanged;
    public RoomSlot Reserve(string patientId)
    {
        if (string.IsNullOrEmpty(patientId)) return null;
        foreach (var r in _rooms) if (r != null && r.owner == patientId) return r;
        foreach (var r in _rooms)
        {
            if (r == null || !string.IsNullOrEmpty(r.owner) || r.entry == null || r.bedApproach == null || r.bedPose == null) continue;
            r.owner = patientId; r.occupied = false; RoomsChanged?.Invoke(); return r;
        }
        return null;
    }
    public bool Occupy(string patientId, int roomNumber)
    {
        foreach (var r in _rooms) if (r != null && r.roomNumber == roomNumber && r.owner == patientId)
        { r.occupied = true; RoomsChanged?.Invoke(); return true; }
        return false;
    }
    public bool ReleaseRoom(string patientId)
    {
        foreach (var r in _rooms) if (r != null && r.owner == patientId)
        { r.owner = null; r.occupied = false; RoomsChanged?.Invoke(); return true; }
        return false;
    }
    public bool ValidateConfiguration(out string error)
    {
        if (_rooms == null || _rooms.Length != 6) { error = "Exactly six room slots are required."; return false; }
        var numbers = new System.Collections.Generic.HashSet<int>();
        foreach (var r in _rooms) if (r == null || r.roomNumber < 1 || r.roomNumber > 6 || !numbers.Add(r.roomNumber)
            || r.entry == null || r.bedApproach == null || r.bedPose == null || r.door == null)
        { error = "Invalid or duplicate room number, missing route/door/bed reference."; return false; }
        error = null; return true;
    }
}
