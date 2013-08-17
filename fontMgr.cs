using UnityEngine;

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;


public class FontMgr
{
	private static FontMgr _Instance;
	//makes class a singleton
	public static FontMgr Instance
	{
		get
		{
			if (_Instance == null)
				new FontMgr();
			return _Instance;
		}
	}
	
	public enum AvailFonts
	{
		Menu=0,
//		Button,
		Paragraph
	};

	private List<Font> FontList;
	

	FontMgr ()
	{
		if (_Instance != null)
			Debug.LogError("Attempt to create multiple FontMgr's");
		_Instance = this;
		FontList = new List<Font>();

		// Menu font
		FontList.Add(new Font("Karloff_40px_4outline"));
		FontList.Add(new Font("pescadero_32px_2outline"));
	}


	public Font GetFont(AvailFonts f)
	{
		return FontList[(int)f];
	}
}





public class Font
{
	//struct that holds all data for one character
	public struct FontInfo
	{
		public int charid;
		public int x;
		public int y;
		public float width;
		public int height;
		public int xoffset;
		public int yoffset;
		public int xadvance;
		public int page;
		public int chnl;
		
	}
	Dictionary<int, FontInfo> FontList;
	
	public int FontHeight;
	public int FontBase;
	private string ResourceName;
	public bool Success;


	public Font (string resourceName)
	{
		ResourceName = resourceName;
		FontHeight = 1;
		// Create container for char info
		FontList = new Dictionary<int,FontInfo> ();
		Success = ReadFont ();
//		BuildWord ("The quick brown fox jumped over the lazy sleeping dog.");
	}
	
	
	private bool ReadFont ()
	{
		string str = "Assets/resources/Fonts/" + ResourceName + ".txt";
		StreamReader sr = new StreamReader (str);
		if (sr == null)
		{
			Debug.LogError("Failed to open font info file: " + str);
			return false;
		}

		// Parse line 1
		string[] seps={" "};
		string line = sr.ReadLine ();
		string[] tokens = line.Split(seps,StringSplitOptions.RemoveEmptyEntries);
		foreach (string s in tokens)
		{
			if (s.StartsWith("size="))
			{
				FontHeight = int.Parse(s.Substring(5));
//				Debug.Log ("Font height: " + FontHeight);
				break;
			}
		}

		// Parse line 2
		line = sr.ReadLine ();
		tokens = line.Split(seps,StringSplitOptions.RemoveEmptyEntries);
		foreach (string s in tokens)
		{
			if (s.StartsWith("base="))
			{
				FontBase = int.Parse(s.Substring(5));
//				Debug.Log ("Font base: " + FontBase);
				break;
			}
		}

		// Bypass lines 3-4 
		sr.ReadLine ();
		sr.ReadLine ();

		// Parse character info
		line = sr.ReadLine ();
		//go through char list line by line
		while (line != null && line.StartsWith("char"))
		{
			FontInfo fi = ReadString (line);
			FontList.Add (fi.charid, fi);
			
			//print(line);
			line = sr.ReadLine ();
		}
		//print(" "+FontList.Count);
		return true;
	}
	

	// reads line and retrieves character data and creates new struct
	private FontInfo ReadString (String line)
	{
		FontInfo chardata = new FontInfo ();
		//char id
		//print(line.Substring(8,3));
		chardata.charid = int.Parse (line.Substring (8, 3));
		//x
		//print(line.Substring(15,3));
		chardata.x = int.Parse (line.Substring (15, 3));
		//y
		//print(line.Substring(23,3));
		chardata.y = int.Parse (line.Substring (23, 3));
		//width
		//print(line.Substring(35,2));
		chardata.width = float.Parse (line.Substring (35, 2));
		//height
		//print(line.Substring(48,2));
		chardata.height = int.Parse (line.Substring (48, 2));
		//xoffset
		//print(line.Substring(62,2));
		chardata.xoffset = int.Parse (line.Substring (62, 2));
		//yoffset
		//print(line.Substring(76,2));
		chardata.yoffset = int.Parse (line.Substring (76, 2));
		//xadvance
		//print(line.Substring(91,2));
		chardata.xadvance = int.Parse (line.Substring (91, 2));
		
		//page=0 & chnl=15 always
		chardata.page = 15;
		chardata.chnl = 15;
		
		return chardata;
	}

	
	
	/// <summary>
	/// Builds one character/letter GameObject.
	/// </summary>
	/// <returns>
	/// The character GameObject or null if font does not include character.
	/// </returns>
	/// <param name='id'>
	/// The character (ASCII code).
	/// </param>
	public GameObject BuildChar (int id)
	{
		GameObject g = null;
		FontInfo val;
		bool ok = FontList.TryGetValue (id, out val);
		if (ok)
		{
			string str = "Fonts/" + ResourceName + "_0";
			g = Geometry.CreateTexturedPlane (str, new Rect (val.x, val.y, val.width, val.height));
			if (g != null)
			{
				float h = g.renderer.bounds.extents.y * 2.0f;
				float heightRatio = (float)val.height / (float)FontHeight;
				float scaleFactor = heightRatio / h;
				g.transform.localScale = new Vector3 (scaleFactor, scaleFactor, 1);
				// Set shader
				g.renderer.material.shader = Shader.Find ("Transparent/Cutout/Diffuse");//NoCull");
				// Tweak alpha cutoff to show more edges
				g.renderer.material.SetFloat ("_Cutoff", 0.3f);
				// Make sure color is white/full alpha
				g.renderer.material.color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
				//	print ("ID:" + id + " gobj h:" + h + " px h:" + val.height + " ratio:" + heightRatio + " final:" + finalHeight);
				g.transform.name = ((char)id).ToString();
			}
			else
				Debug.Log ("Can't find font image resource: " + str);
		}
		return g;
	}


	/// <summary>
	/// Builds a word by creating a GameObject for each character and arranges characters to form a word. The
	/// characters are then all parented to a single word GameObject.
	/// </summary>
	/// <returns>
	/// The word parent GameObject.
	/// </returns>
	/// <param name='word'>
	/// The text to create.
	/// </param>
	public GameObject BuildWord (String word)
	{
		//x position 
		float xpos = 0;
		GameObject [] letters = new GameObject[word.Length];
		
		
		//iterates through word character by character
		int cnt = 0;
		for (int i=0; i<word.Length; i++)
		{
			int ch = word[i];
				
			GameObject g = BuildChar (ch);
			if (g != null)
			{
				FontInfo val;
				FontList.TryGetValue (ch, out val);

				letters[cnt] = g;
				float width = g.renderer.bounds.max.x - g.renderer.bounds.min.x;
			
				//convert xadvance and xoffset to unity units
				float h = g.renderer.bounds.size.y;
	
				float yoff = (float)val.yoffset / (float)FontHeight;

				// Adjust char relative position; -y is down in unity world
				g.transform.Translate (xpos + width / 2, -(yoff + h / 2), 0);
				xpos += (float)(val.xadvance - val.xoffset) / (float)FontHeight;
				// Only count off successful letters
				cnt++;
			}
		}
		// Position parent (to be) at center of entire word
		float cx = (letters[cnt - 1].renderer.bounds.max.x - letters[0].renderer.bounds.min.x) / 2;
		// Parent for characters to allow them to move as a word
		GameObject fuzzwad = new GameObject("[" + word + "]");
		fuzzwad.transform.position = new Vector3 (cx, -0.5f, 0);

		// Assign all letters as child objects
		for (int i=0; i<cnt; i++)
		{
			letters [i].transform.parent = fuzzwad.transform;
		}
		return fuzzwad;
	}
	
}
