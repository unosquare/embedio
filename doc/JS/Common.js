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

var MAX_SECTIONS_COUNT = 50;

var sectionContainerDivIdBase = 'SectionContainerDiv';
var sectionExpanderImgIdBase = 'SectionExpanderImg';

var expandCollapseAllSpanId = 'ExpandCollapseAllSpan';
var expandCollapseAllImgId = 'ExpandCollapseAllImg';

var bigSquareExpandedImgFileName = 'BigSquareExpanded.gif';
var bigSquareCollapsedImgFileName = 'BigSquareCollapsed.gif';
var smallSquareExpandedImgFileName = 'SmallSquareExpanded.gif';
var smallSquareCollapsedImgFileName = 'SmallSquareCollapsed.gif';

var expandAllStr = 'Expand All';
var collapseAllStr = 'Collapse All';

var oldLeftFrameWidth = null;

function SetSectionVisibility(sectionIndex, value)
{
    var div = document.getElementById(sectionContainerDivIdBase + sectionIndex);
    var img = document.getElementById(sectionExpanderImgIdBase + sectionIndex);
    
    if (div && img)
    {
        if (value)
        {
            div.style.visibility = 'visible';
            div.style.display = '';
            
            img.src = ReplaceFileName(img.src, bigSquareExpandedImgFileName);
        }
        else
        {
            div.style.display = 'none';
            div.style.visibility = 'hidden';
            
            img.src = ReplaceFileName(img.src, bigSquareCollapsedImgFileName);
        }
    }
    else
    {
        alert('Error: at SetSectionVisibilityInDoc()');
    }
}

function ToggleSectionVisibility(sectionIndex)
{
    var div = document.getElementById(sectionContainerDivIdBase + sectionIndex);
    var img = document.getElementById(sectionExpanderImgIdBase + sectionIndex);
    
    if (div && img)
    {
        if (div.style.visibility == 'visible' || div.style.visibility == '')
        {
            SetSectionVisibility(sectionIndex, false);
            
            // we've just collapsed a section, so check whether all sections
            // are collapsed and if this is the case, then set ExpandCollapseAllSpan
            // to 'Expand All'
            var allCollapsed = true;
            for (var i = 0; i < MAX_SECTIONS_COUNT; i++)
            {
                var tempDiv = document.getElementById(sectionContainerDivIdBase + i);
                if (tempDiv)
                {
                    if (tempDiv.style.visibility == 'visible' || tempDiv.style.visibility == '')
                    {
                        allCollapsed = false;
                    
                        break;
                    }
                }
            }
            
            if (allCollapsed)
            {
                SetExpandCollapseAllToExpandAll();
            }
        }
        else
        {
            SetSectionVisibility(sectionIndex, true);
            
            // set ExpandCollapseAllSpan to 'Collapse All' because we've just
            // expanded some section
            SetExpandCollapseAllToCollapseAll();
        }
    }
    else
    {
        alert('Error: at ToggleSectionVisibility()');
    }
}

function ToggleAllSectionsVisibility()
{
    var span = document.getElementById(expandCollapseAllSpanId);
    var img = document.getElementById(expandCollapseAllImgId);
    var visibliity;
    
    if (span && img)
    {
        if (span.innerHTML == expandAllStr)
        {
            span.innerHTML = collapseAllStr;
            img.src = ReplaceFileName(img.src, smallSquareExpandedImgFileName);
            
            visibility = true;
        }
        else
        {
            span.innerHTML = expandAllStr;
            img.src = ReplaceFileName(img.src, smallSquareCollapsedImgFileName);
            
            visibility = false;
        }
        
        for (var i = 0; i < MAX_SECTIONS_COUNT; i++)
        {
            if (document.getElementById(sectionContainerDivIdBase + i))
            {
                SetSectionVisibility(i, visibility);
            }
        }
    }
    else
    {
        alert('Error: at ToggleSectionVisibility()');
    }
}

function SetExpandCollapseAllToCollapseAll()
{
    var span = document.getElementById(expandCollapseAllSpanId);
    var img = document.getElementById(expandCollapseAllImgId);

    if (span && img)
    {
        span.innerHTML = collapseAllStr;
        img.src = ReplaceFileName(img.src, smallSquareExpandedImgFileName);
    }
    else
    {
        alert('Error: at SetExpandCollapseAllToCollapseAll()');
    }
}

function SetExpandCollapseAllToExpandAll()
{
    var span = document.getElementById(expandCollapseAllSpanId);
    var img = document.getElementById(expandCollapseAllImgId);

    if (span && img)
    {
        span.innerHTML = expandAllStr;
        img.src = ReplaceFileName(img.src, smallSquareCollapsedImgFileName);
    }
    else
    {
        alert('Error: at ToggleSectionVisibility() {1}');
    }
}

function ReplaceFileName(filePath, fileName)
{
    var ch = '/';
    var index = filePath.lastIndexOf(ch);
    
    if (index == -1)
    {
        ch = '\\';
        index = filePath.lastIndexOf(ch);
    }
    
    if (index == -1)
    {
        return fileName;
    }
    else
    {
        return filePath.substring(0, index) + ch + fileName;
    }
}

function ToggleLeftFrame()
{
    var frameset = parent.document.getElementById("Frameset")

    if (!frameset)
    {
        alert("Error: at ToggleLeftFrame(): Couldn't obtain reference to the frameset element.");
        return;
    }

    var contentsDiv = document.getElementById("Contents");
    
    if (!contentsDiv)
    {
        alert("Error: at ToggleLeftFrame(): Couldn't obtain reference to the Contents div.");
        return;
    }
    
    var headerTitleDiv = document.getElementById("HeaderTitle");
    
    if (!headerTitleDiv)
    {
        alert("Error: at ToggleLeftFrame(): Couldn't obtain reference to the HeaderTitle div.");
        return;
    }
    
    var switcherImg = document.getElementById("LeftMenuSwitcherLink");
    
    if (!switcherImg)
    {
        alert("Error: at ToggleLeftFrame(): Couldn't obtain reference to the LeftMenuSwitcherLink div.");
        return;
    }

    if (contentsDiv.style.visibility == 'visible' || contentsDiv.style.visibility == '')
    {
        var splitted = frameset.cols.split(',');
        if (splitted.length != 2)
        {
            alert("Error: at ToggleLeftFrame(): Wrong format of frameset.cols attribute.");
            return;
        }
        
        oldLeftFrameWidth = splitted[0];
        
        frameset.cols = "20,*";

        var leftFrame = parent.document.getElementById("LeftMenuFrame");
        if (!leftFrame)
        {
            alert("Error: at ToggleLeftFrame(): Couldn't obtain reference to the left menu frame.");
            return;
        }
        leftFrame.noResize = true;

        contentsDiv.style.visibility = 'hidden';
        contentsDiv.style.display = 'none';
        headerTitleDiv.style.visibility = 'hidden';
        switcherImg.src = ReplaceFileName(switcherImg.src, 'RightArrow.gif');
    }
    else
    {
        if (!oldLeftFrameWidth)
        {
            alert("Error: at ToggleLeftFrame(): Couldn't restore the width of the left frame.");
            return;
        }
        else
        {
            frameset.cols = oldLeftFrameWidth + ",*";
        }
        
        var leftFrame = parent.document.getElementById("LeftMenuFrame");
        if (!leftFrame)
        {
            alert("Error: at ToggleLeftFrame(): Couldn't obtain reference to the left menu frame.");
            return;
        }
        leftFrame.noResize = false;

        contentsDiv.style.visibility = 'visible';
        contentsDiv.style.display = 'block';
        headerTitleDiv.style.visibility = 'visible';
        switcherImg.src = ReplaceFileName(switcherImg.src, 'LeftArrow.gif');
    }
}
