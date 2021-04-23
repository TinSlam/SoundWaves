# SoundWaves
A Wave-Based Simulation of Sound

To run the simulation using <b>Unity</b>, download the unitypackage file and import it using <b>Assets->Import->Custom Package</b>.<br />
Load the scene and play the simulation. You can use the inspector to play around with the parameters.

<b>SoundWaves Gameobject:</b> Parameters of the simulation<br />
<b>SoundSource Gameobject:</b> Parameters of each individual sound source

<h2>Do not have Unity installed?</h2>
There are executables for Windows and Linux available in the <b>Executables</b> directory. However, you do not access to the inspector to
change the parameters of the model and the sound sources.<br />
If interested in the code, you can find the scripts inside <b>scripts</b> folder.

<b>Known Bugs</b><br />
(I) Discretization effects for diffraction<br />
(II) Diffraction corners work, holes do not<br />
(III) Diffraction sources become persistent sometimes<br />
