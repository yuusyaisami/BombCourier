using System.Collections.Generic;
using UnityEngine;

namespace BC.Base
{
    public sealed class MoveContactBuffer
    {
        private readonly List<MoveContactInfo> contacts = new List<MoveContactInfo>(32);

        public IReadOnlyList<MoveContactInfo> Contacts => contacts;

        public int Count => contacts.Count;

        public void ClearForNextPhysicsTick()
        {
            contacts.Clear();
        }

        public void Add(in MoveContactInfo contactInfo)
        {
            contacts.Add(contactInfo);
        }

        public void AddDirect(MoveContactInfo contactInfo)
        {
            contacts.Add(contactInfo);
        }

        public void Add(Collision collision, MovementBodyGeometry geometry)
        {
            if (collision == null || collision.collider == null)
                return;

            int contactCount = collision.contactCount;
            if (contactCount <= 0)
                return;

            for (int i = 0; i < contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                float upDot = Vector3.Dot(contact.normal, Vector3.up);
                float angle = Vector3.Angle(contact.normal, Vector3.up);

                MoveContactInfo info = new MoveContactInfo
                {
                    Collider = collision.collider,
                    AttachedRigidbody = collision.rigidbody,
                    Transform = collision.collider.transform,
                    Point = contact.point,
                    Normal = contact.normal,
                    UpDot = upDot,
                    Angle = angle,
                    RelativeVelocity = collision.relativeVelocity,
                    RelativeSpeed = collision.relativeVelocity.magnitude,
                    Kind = MoveContactKind.None,
                };

                contacts.Add(info);
            }
        }

        public MoveContactInfo Get(int index)
        {
            return contacts[index];
        }

        public void Set(int index, in MoveContactInfo contactInfo)
        {
            contacts[index] = contactInfo;
        }
    }
}
