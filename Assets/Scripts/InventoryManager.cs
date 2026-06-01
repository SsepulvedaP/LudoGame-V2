using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    private GameObject[] bag;
    [SerializeField] private SelectManager selector;
    [SerializeField] private Image[] image;
    [SerializeField] private Sprite[] resources;
#pragma warning disable 0414
    private bool bagInUse;
#pragma warning restore 0414

    private void Update()
    {
        bag = selector.TemporaryInventory();
        
        for (int i = 0; i < image.Length; i++)
        {
            if (image[i] == null) continue;

            if (bag != null && i < bag.Length && bag[i] != null)
            {
                if (bag[i].TryGetComponent<PickableManager>(out var pickable))
                {
                    image[i].sprite = pickable.ItemSprite;
                    image[i].enabled = pickable.ItemSprite != null;
                }
                else
                {
                    image[i].enabled = false;
                }
            }
            else
            {
                image[i].sprite = null;
                image[i].enabled = false;
            }
        }

        bool keyGPressed = false;
        if (Keyboard.current != null)
        {
            keyGPressed = Keyboard.current.gKey.isPressed;
        }

        if (keyGPressed) //Borrar cuando ya este todo implementado. esto es solo de verificacion
        {
            if (bag != null)
            {
                for (int i = 0; i < bag.Length; i++)
                {
                    if (bag[i] != null)
                    {
                        Debug.Log(bag[i].name);
                    }
                }
            }
        }
    }

    public void BagUsed()
    {
        bagInUse = true;
    }
    public void BagEmpty()
    {
        bagInUse = false;
    }
}
