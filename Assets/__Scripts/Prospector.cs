﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class Prospector : MonoBehaviour
{

    static public Prospector S;

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public float xOffset = 3;
    public float yOffset = -2.5f;
    public Vector3 layoutCenter;


    [Header("Set Dynamically")]
    public Deck deck;
    public Layout layout;
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;

    void Awake()
    {
        S = this; //Set up a Singleton for Prospector
    }

    void Start()
    {
        deck = GetComponent<Deck>(); //Get the Deck
        deck.InitDeck(deckXML.text); //Pass DeckXML to it
        Deck.Shuffle(ref deck.cards);

        layout = GetComponent<Layout>();
        layout.ReadLayout(layoutXML.text);
        drawPile = ConvertListCardsToListCardProspectors(deck.cards);
        LayoutGame();

    }

    List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD)
    {
        List<CardProspector> lCP = new List<CardProspector>();
        CardProspector tCP;
        foreach (Card tCD in lCD)
        {
            tCP = tCD as CardProspector;
            lCP.Add(tCP);
        }
        return (lCP);
    }

    CardProspector Draw()
    {
        CardProspector cd = drawPile[0];
        drawPile.RemoveAt(0);
        return (cd);
    }

    void LayoutGame()
    {
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }
        CardProspector cp;
        foreach (SlotDef tSD in layout.slotDefs)
        {
            cp = Draw();
            cp.faceUp = tSD.faceUp;
            cp.transform.parent = layoutAnchor;
            cp.transform.localPosition = new Vector3(layout.multiplier.x * tSD.x, layout.multiplier.y * tSD.y, -tSD.layerID);
            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            cp.state = eCardState.tableau;
            cp.SetSortingLayerName(tSD.layerName);
            tableau.Add(cp);
        }
        foreach (CardProspector tCP in tableau)
        {
            foreach (int hid in tCP.slotDef.hiddenBy)
            {
                cp = FindCardByLayoutID(hid);
                tCP.hiddenBy.Add(cp);
            }
        }
        MoveToTarget(Draw());
        UpdateDrawPile();
    }
    CardProspector FindCardByLayoutID(int layoutID)
    {
        foreach (CardProspector tCP in tableau)
        {
            if (tCP.layoutID == layoutID)
            {
                return (tCP);
            }
        }
        return (null);
    }

    void SetTableauFaces()
    {
        foreach (CardProspector cd in tableau)
        {
            bool faceUp = true;
            foreach (CardProspector cover in cd.hiddenBy)
            {
                if (cover.state == eCardState.tableau)
                {
                    faceUp = false;
                }
            }
            cd.faceUp = faceUp;
        }
    }

    void MoveToDiscard(CardProspector cd)
    {
        cd.state = eCardState.discard;
        discardPile.Add(cd);
        cd.transform.parent = layoutAnchor;
        cd.transform.localPosition = new Vector3(layout.multiplier.x * layout.discardPile.x, layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID + 0.5f);
        cd.faceUp = true;
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }

    void MoveToTarget(CardProspector cd)
    {
        if (target != null) MoveToDiscard(target);
        target = cd;
        cd.state = eCardState.target;
        cd.transform.parent = layoutAnchor;
        cd.transform.localPosition = new Vector3(layout.multiplier.x * layout.discardPile.x, layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID);
        cd.faceUp = true;
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);
    }
    void UpdateDrawPile()
    {
        CardProspector cd;
        for (int i = 0; i < drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;
            Vector2 dpStagger = layout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x), layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y), -layout.drawPile.layerID + 0.1f * i);
            cd.faceUp = false;
            cd.state = eCardState.drawpile;
            cd.SetSortingLayerName(layout.drawPile.layerName);
            cd.SetSortOrder(-10 * i);
        }
    }

    public void CardClicked(CardProspector cd)
    {
        switch (cd.state)
        {
            case eCardState.target:
                break;
            case eCardState.drawpile:
                MoveToDiscard(target);
                MoveToTarget(Draw());
                UpdateDrawPile();
                break;
            case eCardState.tableau:
                bool validMatch = true;
                if (!cd.faceUp)
                {
                    validMatch = false;
                }
                if (!AdjacentRank(cd, target))
                {
                    validMatch = false;
                }
                if (!validMatch) return;
                tableau.Remove(cd);
                MoveToTarget(cd);
                SetTableauFaces();
                break;
        }
        CheckForGameOver();
    }

    void CheckForGameOver()
    {
        if (tableau.Count == 0)
        {
            GameOver(true);
            return;
        }
        if (drawPile.Count > 0)
        {
            return;
        }
        foreach (CardProspector cd in tableau)
        {
            if (AdjacentRank(cd, target))
            {
                return;
            }
        }
        GameOver(false);
    }
    void GameOver(bool won)
    {
        if (won)
        {
            print("Game Over. You won! :)");
        }
        else
        {
            print("Game Over. You Lost. :(");
        }
        SceneManager.LoadScene("__Prospector_Scene_0");
    }

    public bool AdjacentRank(CardProspector c0, CardProspector cl)
    {
        if (!c0.faceUp || !cl.faceUp) return (false);
        if (Mathf.Abs(c0.rank - cl.rank) == 1)
        {
            return (true);
        }
        if (c0.rank == 1 && cl.rank == 13) return (true);
        if (c0.rank == 13 && cl.rank == 1) return (true);
        return (false);
    }
}