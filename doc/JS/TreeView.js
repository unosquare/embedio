/*
 * Copyright 2007 - 2009 Marek St�j
 * 
 * This file is part of ImmDoc .NET.
 *
 * ImmDoc .NET is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * ImmDoc .NET is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ImmDoc .NET; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

var lastSelectedAElement = null;

function TV_Node_Clicked(subtreePath)
{
    var subtreeDiv = document.getElementById('TV_Subtree_' + subtreePath);
    var iconImg = document.getElementById('TV_NodeExpansionIcon_' + subtreePath);
    
    if (!subtreeDiv || !iconImg)
    {
        alert("Error: At TV_Node_Clicked(): Couldn't obtain reference to appropriate subtree or node expansion icon.");
        return;
    }
    
    if (GetStyleValue(subtreeDiv, 'display') == 'none')
    {
        TV_ToggleSubtreeVisibility(subtreeDiv, iconImg, true);
    }
    else
    {
        TV_ToggleSubtreeVisibility(subtreeDiv, iconImg, false);
    }
}

function TV_ToggleSubtreeVisibility(subtreeDiv, iconImg, value)
{
    if (value)
    {
        subtreeDiv.style.visibility = 'visible';
        subtreeDiv.style.display = 'block';
        
        iconImg.src = ReplaceFileName(iconImg.src, 'TV_Minus.gif');
    }
    else
    {
        subtreeDiv.style.visibility = 'hidden';
        subtreeDiv.style.display = 'none';
        
        iconImg.src = ReplaceFileName(iconImg.src, 'TV_Plus.gif');
    }
}

function TV_NodeLink_Clicked(aElement, subtreePath)
{
    if (!aElement)
    {
        alert("Error: Undefine link object.");
        return;
    }

    if (!lastSelectedAElement)
    {
        lastSelectedAElement = document.getElementById('TV_RootNode');
        if (!lastSelectedAElement)
        {
            alert("Error: Couldn't find root node.");
            return;
        }
    }

    lastSelectedAElement.className = 'TV_NodeLink';
    lastSelectedAElement = aElement;
    aElement.className = 'TV_NodeLink_Selected';
    
    if (subtreePath)
    {
        var subtreeDiv = document.getElementById('TV_Subtree_' + subtreePath);
        var iconImg = document.getElementById('TV_NodeExpansionIcon_' + subtreePath);
        
        if (subtreeDiv && iconImg)
        {
            TV_ToggleSubtreeVisibility(subtreeDiv, iconImg, true);
        }
    }
}
