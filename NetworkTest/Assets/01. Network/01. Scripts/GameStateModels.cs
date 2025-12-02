using System.Collections.Generic;
using System.IO;
using UnityEngine;

// JSON message for customization updates
[System.Serializable]
public class PlayerCustomizationMessage
{
    public string type = "player_customization";
    public byte playerId;
    public bool isMale;
    public ModelInfo modelInfo;
}


// Helper for packing boolean values into a single byte
public static class AnimationBitmask
{
    public const byte Walk = 1 << 0;       // 1
    public const byte Sprint = 1 << 1;     // 2
    public const byte Roll = 1 << 2;       // 4
    public const byte IsGrounded = 1 << 3; // 8
    public const byte Crouch = 1 << 4;     // 16
    public const byte UnarmedAttackJab = 1 << 5; // 32
    public const byte UnarmedAttackCross = 1 << 6; // 64
    public const byte Sit = 1 << 7; // 128

    public static bool IsSet(byte mask, byte flag) => (mask & flag) == flag;
}

// Enum to identify the type of monster
public enum MonsterType : byte // Use byte for network efficiency
{
    GellyCube,
    GellySphere,
    Golem,
    Minotaur,
    PlantMonster,
    Gazer
}

// Optimized data structure for a single player's state
public struct PlayerState
{
    public byte playerId;
    public Vector3 position;
    public Quaternion rotation;
    public Quaternion cameraRotation; // For first-person camera pitch
    public byte animationMask;
    public float moveX;
    public float moveY;
    public int weaponId;
    public float bending;
    public bool isMale;
    public ModelInfo modelInfo;

    public byte[] ToByteArray()
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(playerId);
            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write(position.z);
            writer.Write(rotation.x);
            writer.Write(rotation.y);
            writer.Write(rotation.z);
            writer.Write(rotation.w);
            writer.Write(cameraRotation.x);
            writer.Write(cameraRotation.y);
            writer.Write(cameraRotation.z);
            writer.Write(cameraRotation.w);
            writer.Write(animationMask);
            writer.Write(moveX);
            writer.Write(moveY);
            writer.Write(weaponId);
            writer.Write(bending);
            writer.Write(isMale);
            writer.Write(modelInfo.head);
            writer.Write(modelInfo.body);
            writer.Write(modelInfo.acc1);
            writer.Write(modelInfo.acc2);
            return stream.ToArray();
        }
    }

    public static PlayerState FromBytes(byte[] data)
    {
        var state = new PlayerState();
        using (MemoryStream stream = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            state.playerId = reader.ReadByte();
            state.position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            state.rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            state.cameraRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            state.animationMask = reader.ReadByte();
            state.moveX = reader.ReadSingle();
            state.moveY = reader.ReadSingle();
            state.weaponId = reader.ReadInt32();
            state.bending = reader.ReadSingle();
            state.isMale = reader.ReadBoolean();
            state.modelInfo = new ModelInfo(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32()
            );
        }
        return state;
    }
}

// Optimized data structure for a single monster's state
public struct MonsterState
{
    public ushort monsterId;
    public MonsterType monsterType; // Added monster type
    public Vector3 position;
    public Quaternion rotation;
    public float currentHP;
    public float maxHP;
    public byte[] animationData;

    public byte[] ToByteArray()
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(monsterId);
            writer.Write((byte)monsterType);
            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write(position.z);
            writer.Write(rotation.x);
            writer.Write(rotation.y);
            writer.Write(rotation.z);
            writer.Write(rotation.w);
            writer.Write(currentHP);
            writer.Write(maxHP);

            byte dataLength = (byte)(animationData?.Length ?? 0);
            writer.Write(dataLength);
            if (dataLength > 0)
            {
                writer.Write(animationData);
            }
            return stream.ToArray();
        }
    }

    public static MonsterState FromBytes(byte[] data)
    {
        var state = new MonsterState();
        using (MemoryStream stream = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            state.monsterId = reader.ReadUInt16();
            state.monsterType = (MonsterType)reader.ReadByte();
            state.position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            state.rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            state.currentHP = reader.ReadSingle();
            state.maxHP = reader.ReadSingle();

            byte dataLength = reader.ReadByte();
            if (dataLength > 0)
            {
                state.animationData = reader.ReadBytes(dataLength);
            }
        }
        return state;
    }
}

// The main container for all real-time game data
public class NetworkGameState
{
    public List<PlayerState> players = new List<PlayerState>();
    public int selectedPlanetId = -1; // Default to -1, indicating no planet is selected
    public bool isShipLanded;
    public bool isShipDoorOpen;

    private byte GetShipStateMask()
    {
        byte mask = 0;
        if (isShipLanded) mask |= 1 << 0;
        if (isShipDoorOpen) mask |= 1 << 1;
        return mask;
    }

    // --- Serialization (Host) ---
    public byte[] ToByteArray()
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            // Write player states
            writer.Write((byte)players.Count);
            foreach (var p in players)
            {
                writer.Write(p.playerId);
                writer.Write(p.position.x);
                writer.Write(p.position.y);
                writer.Write(p.position.z);
                writer.Write(p.rotation.x);
                writer.Write(p.rotation.y);
                writer.Write(p.rotation.z);
                writer.Write(p.rotation.w);
                writer.Write(p.cameraRotation.x);
                writer.Write(p.cameraRotation.y);
                writer.Write(p.cameraRotation.z);
                writer.Write(p.cameraRotation.w);
                writer.Write(p.animationMask);
                writer.Write(p.moveX);
                writer.Write(p.moveY);
                writer.Write(p.weaponId);
                writer.Write(p.bending);
                writer.Write(p.isMale);
                writer.Write(p.modelInfo.head);
                writer.Write(p.modelInfo.body);
                writer.Write(p.modelInfo.acc1);
                writer.Write(p.modelInfo.acc2);
            }

            // Write game-level state
            writer.Write(selectedPlanetId);
            writer.Write(GetShipStateMask());

            return stream.ToArray();
        }
    }

    // --- Deserialization (Client) ---
    public static NetworkGameState FromBytes(byte[] data)
    {
        var gameState = new NetworkGameState();
        using (MemoryStream stream = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            // Read player states
            byte playerCount = reader.ReadByte();
            for (int i = 0; i < playerCount; i++)
            {
                var p = new PlayerState();
                p.playerId = reader.ReadByte();
                p.position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                p.rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                p.cameraRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                p.animationMask = reader.ReadByte();
                p.moveX = reader.ReadSingle();
                p.moveY = reader.ReadSingle();
                p.weaponId = reader.ReadInt32();
                p.bending = reader.ReadSingle();

                if (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    p.isMale = reader.ReadBoolean();
                    p.modelInfo = new ModelInfo(
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadInt32()
                    );
                }
                gameState.players.Add(p);
            }

            // Read game-level state
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                gameState.selectedPlanetId = reader.ReadInt32();
            }
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte shipStateMask = reader.ReadByte();
                gameState.isShipLanded = (shipStateMask & (1 << 0)) != 0;
                gameState.isShipDoorOpen = (shipStateMask & (1 << 1)) != 0;
            }
        }
        return gameState;
    }
}

// A dedicated container for monster state updates
public class NetworkMonsterUpdateState
{
    public List<MonsterState> monsters = new List<MonsterState>();

    public byte[] ToByteArray()
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write((byte)monsters.Count);
            foreach (var m in monsters)
            {
                writer.Write(m.monsterId);
                writer.Write((byte)m.monsterType);
                writer.Write(m.position.x);
                writer.Write(m.position.y);
                writer.Write(m.position.z);
                writer.Write(m.rotation.x);
                writer.Write(m.rotation.y);
                writer.Write(m.rotation.z);
                writer.Write(m.rotation.w);
                writer.Write(m.currentHP);
                writer.Write(m.maxHP);

                byte dataLength = (byte)(m.animationData?.Length ?? 0);
                writer.Write(dataLength);
                if (dataLength > 0)
                {
                    writer.Write(m.animationData);
                }
            }
            return stream.ToArray();
        }
    }

    public static NetworkMonsterUpdateState FromBytes(byte[] data)
    {
        var updateState = new NetworkMonsterUpdateState();
        using (MemoryStream stream = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            byte monsterCount = reader.ReadByte();
            for (int i = 0; i < monsterCount; i++)
            {
                var m = new MonsterState
                {
                    monsterId = reader.ReadUInt16(),
                    monsterType = (MonsterType)reader.ReadByte(),
                    position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                    rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                    currentHP = reader.ReadSingle(),
                    maxHP = reader.ReadSingle()
                };

                byte dataLength = reader.ReadByte();
                if (dataLength > 0)
                {
                    m.animationData = reader.ReadBytes(dataLength);
                }
                updateState.monsters.Add(m);
            }
        }
        return updateState;
    }
}
