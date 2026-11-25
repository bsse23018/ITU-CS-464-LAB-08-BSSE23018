"""Generate the original CC0 soundtrack and synthesized cues for Lab 08."""
from math import exp, pi, sin
from pathlib import Path
from random import Random
import struct
import wave

RATE = 22050
OUTPUT = Path(__file__).resolve().parents[1] / "Assets/Resources/Lab08Audio"
OUTPUT.mkdir(parents=True, exist_ok=True)


def write(name, samples):
    pcm = [round(max(-1, min(1, sample)) * 32767) for sample in samples]
    with wave.open(str(OUTPUT / f"{name}.wav"), "wb") as audio:
        audio.setparams((1, 2, RATE, len(pcm), "NONE", "not compressed"))
        audio.writeframes(struct.pack(f"<{len(pcm)}h", *pcm))


def note(freq, duration, gain=.27, harmonic=.22):
    count = round(duration * RATE)
    for i in range(count):
        phase = 2 * pi * freq * i / RATE
        env = min(1, i / 110, (count - i) / 220) * exp(-4 * i / count)
        yield gain * env * (sin(phase) + harmonic * sin(2 * phase))


CUES = {
    "select": [(830, .055)],
    "click": [(550, .065)],
    "back": [(660, .055), (440, .09)],
    "open": [(400, .07), (600, .12)],
    "close": [(480, .07), (320, .11)],
    "pause": [(520, .105), (310, .15)],
    "resume": [(310, .08), (520, .08), (710, .14)],
    "success": [(660, .075), (880, .10), (990, .18)],
    "error": [(211, .105), (180, .15)],
    "launch": [(260, .085), (392, .085), (523, .26)],
    "pickup": [(1046, .064), (1568, .14)],
    "shield": [(800, .07), (380, .08), (188, .2)],
    "win": [(523, .12), (660, .12), (784, .12), (1046, .33)],
    "lose": [(392, .18), (330, .18), (196, .37)],
}
for cue, melody in CUES.items():
    write(cue, [sample for freq, duration in melody for sample in note(freq, duration)])

rng = Random(8)
noise = 0
hit = []
for i in range(round(.27 * RATE)):
    t = i / RATE
    noise += (rng.uniform(-1, 1) - noise) * .22
    hit.append((.56 * noise + .42 * sin(2 * pi * (124 * t - 95 * t * t))) * exp(-19 * t) * min(1, t / .004))
write("hit", hit)


def music(name, beat, pattern, bright):
    loop = [0.] * round(16 * beat * RATE)

    def add(start, hz, duration, gain, harmonic):
        offset = round(start * RATE)
        for i, sample in enumerate(note(hz, duration, gain, harmonic)):
            loop[(offset + i) % len(loop)] += sample

    for bar, chord in enumerate(pattern):
        start = bar * 4 * beat
        if bright:
            for step, degree in enumerate((0, 1, 2, 1, 0, 2, 1, 2)):
                add(start + step * beat / 2, chord[degree] * 2, beat * .58, .14, .35)
            for step in range(4):
                add(start + step * beat, chord[0] / 2, beat * .8, .23, .54)
                add(start + step * beat, 53, .09, .16, .08)
        else:
            for step, degree in enumerate((0, 2, 1, 2)):
                add(start + step * beat, chord[degree], beat * 1.7, .18, .1)
            add(start, chord[0] / 2, beat * 3.5, .21, .1)

    write(name, loop)


music("music", .72, [(220, 261.63, 329.63), (174.61, 220, 261.63),
                      (196, 246.94, 293.66), (164.81, 207.65, 246.94)], False)
music("flight_music", .46, [(220, 261.63, 329.63), (261.63, 329.63, 392),
                              (196, 246.94, 293.66), (164.81, 207.65, 246.94)], True)
