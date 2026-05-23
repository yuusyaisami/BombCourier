using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.Managers
{
    public sealed class CharacterDataBaseService
    {
        private readonly Dictionary<CharacterIdReference, RegistrationEntry> registrationsByCharacter = new();
        private readonly Dictionary<TalkAdapterMB, CharacterIdReference> registrationsByAdapter = new();

        public void Register(CharacterIdReference characterId, EntityRef entity, TalkAdapterMB adapter)
        {
            if (!characterId.IsAssigned || !entity.IsValid || adapter == null)
                return;

            CharacterIdReference normalizedCharacter = Normalize(characterId);

            if (registrationsByCharacter.TryGetValue(normalizedCharacter, out RegistrationEntry existing) &&
                existing.Adapter != null &&
                existing.Adapter != adapter)
            {
                Debug.LogWarning(
                    $"{nameof(CharacterDataBaseService)}: Character '{normalizedCharacter}' was already registered by '{existing.Adapter.name}'. Replacing with '{adapter.name}'.",
                    adapter);
                registrationsByAdapter.Remove(existing.Adapter);
            }

            if (registrationsByAdapter.TryGetValue(adapter, out CharacterIdReference previousCharacter))
                registrationsByCharacter.Remove(previousCharacter);

            registrationsByCharacter[normalizedCharacter] = new RegistrationEntry(normalizedCharacter, entity, adapter);
            registrationsByAdapter[adapter] = normalizedCharacter;
        }

        public void Unregister(TalkAdapterMB adapter)
        {
            if (adapter == null)
                return;

            if (!registrationsByAdapter.TryGetValue(adapter, out CharacterIdReference character))
                return;

            registrationsByAdapter.Remove(adapter);

            if (registrationsByCharacter.TryGetValue(character, out RegistrationEntry existing) &&
                existing.Adapter == adapter)
            {
                registrationsByCharacter.Remove(character);
            }
        }

        public bool TryResolveTalkAdapter(
            CharacterIdReference characterId,
            SceneKernel sceneKernel,
            out TalkAdapterMB adapter,
            out EntityRef entity)
        {
            adapter = null;
            entity = default;

            if (!characterId.IsAssigned)
                return false;

            CharacterIdReference normalizedCharacter = Normalize(characterId);

            if (!registrationsByCharacter.TryGetValue(normalizedCharacter, out RegistrationEntry entry))
                return false;

            if (!IsEntryAlive(entry, sceneKernel, out TalkAdapterMB resolvedAdapter))
            {
                registrationsByCharacter.Remove(normalizedCharacter);

                if (entry.Adapter != null)
                    registrationsByAdapter.Remove(entry.Adapter);

                return false;
            }

            adapter = resolvedAdapter;
            entity = entry.Entity;
            return true;
        }

        private static bool IsEntryAlive(RegistrationEntry entry, SceneKernel sceneKernel, out TalkAdapterMB resolvedAdapter)
        {
            resolvedAdapter = null;

            if (entry.Adapter == null || !entry.Entity.IsValid)
                return false;

            if (sceneKernel?.EntityComponents == null)
            {
                resolvedAdapter = entry.Adapter;
                return true;
            }

            if (!sceneKernel.EntityComponents.TryResolve(entry.Entity, out TalkAdapterMB adapterFromEntity))
                return false;

            if (adapterFromEntity == null)
                return false;

            if (!ReferenceEquals(adapterFromEntity, entry.Adapter))
                return false;

            resolvedAdapter = adapterFromEntity;
            return true;
        }

        private static CharacterIdReference Normalize(CharacterIdReference characterId)
        {
            if (CharacterIdRegistry.TryGetDescriptor(characterId, out CharacterIdDescriptor descriptor))
            {
                // Keep id/path canonical to avoid duplicate-key drift from legacy path-only values.
                return CharacterIdReference.From(descriptor.Id);
            }

            return characterId;
        }

        private readonly struct RegistrationEntry
        {
            public RegistrationEntry(CharacterIdReference characterId, EntityRef entity, TalkAdapterMB adapter)
            {
                CharacterId = characterId;
                Entity = entity;
                Adapter = adapter;
            }

            public CharacterIdReference CharacterId { get; }
            public EntityRef Entity { get; }
            public TalkAdapterMB Adapter { get; }
        }
    }
}
