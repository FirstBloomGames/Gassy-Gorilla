#!/usr/bin/env python3
"""Build the licensed Gassy Gorilla comedic boost and sputter families."""

from __future__ import annotations

import argparse
import math
import random
import struct
import tempfile
import urllib.request
import wave
from pathlib import Path


SAMPLE_RATE = 32000
SOURCES = {
    "cartoon_fart": (
        "cartoon_fart.wav",
        "https://res.cloudinary.com/dol86wsz1/video/upload/v1782164731/"
        "summer_art/sfx_short_funny_cartoon_fart_sound_.wav",
    ),
    "squeak_pop": (
        "squeak_pop.wav",
        "https://res.cloudinary.com/dol86wsz1/video/upload/v1783620338/"
        "summer_art/4117f140-79b7-4147-9ed6-931cb56040bf_"
        "sfx_short_one_shot_comical_squeak__1783620338057.wav",
    ),
    "juicy_pop": (
        "juicy_pop.wav",
        "https://res.cloudinary.com/dol86wsz1/video/upload/v1783805236/"
        "summer_art/1f84df03-4216-4685-8a04-33f505dc2d72_"
        "sfx_short_one_shot_juicy_cartoon__1783805234965.wav",
    ),
    "rubber_boing": (
        "rubber_boing.wav",
        "https://res.cloudinary.com/dol86wsz1/video/upload/v1783805236/"
        "summer_art/1f84df03-4216-4685-8a04-33f505dc2d72_"
        "sfx_short_one_shot_soft_comedic_b__1783805233908.wav",
    ),
    "hero_whoosh": (
        "hero_whoosh.wav",
        "https://res.cloudinary.com/dol86wsz1/video/upload/v1781920148/"
        "summer_art/sfx_short_one_shot_cartoon_superh_.wav",
    ),
    "cork_potion": (
        "cork_potion.wav",
        "https://res.cloudinary.com/dol86wsz1/video/upload/v1784284046/"
        "summer_art/ff78a2c2-2309-421f-b465-1a948efd67e5_"
        "sfx_a_potion_being_drunk_a_cork__1784284045269.wav",
    ),
}


def clamp(value: float, low: float = -1.0, high: float = 1.0) -> float:
    return max(low, min(high, value))


def read_pcm16_mono(path: Path) -> tuple[list[float], int]:
    with wave.open(str(path), "rb") as reader:
        channels = reader.getnchannels()
        sample_width = reader.getsampwidth()
        sample_rate = reader.getframerate()
        frame_count = reader.getnframes()
        raw = reader.readframes(frame_count)

    if sample_width != 2:
        raise ValueError(f"{path} must be 16-bit PCM WAV")

    values = struct.unpack(f"<{len(raw) // 2}h", raw)
    if channels == 1:
        samples = [value / 32768.0 for value in values]
    else:
        samples = []
        for frame in range(frame_count):
            offset = frame * channels
            samples.append(
                sum(values[offset : offset + channels]) / (32768.0 * channels)
            )
    return samples, sample_rate


def resample(samples: list[float], source_rate: int, target_rate: int) -> list[float]:
    if source_rate == target_rate or not samples:
        return list(samples)

    output_length = max(1, round(len(samples) * target_rate / source_rate))
    scale = source_rate / target_rate
    output = [0.0] * output_length
    for index in range(output_length):
        position = index * scale
        lower = min(int(position), len(samples) - 1)
        upper = min(lower + 1, len(samples) - 1)
        fraction = position - lower
        output[index] = samples[lower] + (samples[upper] - samples[lower]) * fraction
    return output


def trim_silence(
    samples: list[float], threshold: float = 0.004, padding_seconds: float = 0.004
) -> list[float]:
    active = [index for index, sample in enumerate(samples) if abs(sample) >= threshold]
    if not active:
        return list(samples)

    padding = round(padding_seconds * SAMPLE_RATE)
    start = max(0, active[0] - padding)
    end = min(len(samples), active[-1] + padding + 1)
    return samples[start:end]


def crop(samples: list[float], start: float, duration: float) -> list[float]:
    first = max(0, round(start * SAMPLE_RATE))
    last = min(len(samples), first + round(duration * SAMPLE_RATE))
    return samples[first:last]


def change_rate(samples: list[float], factor: float) -> list[float]:
    if not samples or abs(factor - 1.0) < 0.0001:
        return list(samples)

    output_length = max(1, round(len(samples) / factor))
    output = [0.0] * output_length
    for index in range(output_length):
        position = index * factor
        lower = min(int(position), len(samples) - 1)
        upper = min(lower + 1, len(samples) - 1)
        fraction = position - lower
        output[index] = samples[lower] + (samples[upper] - samples[lower]) * fraction
    return output


def low_pass(samples: list[float], cutoff: float) -> list[float]:
    if not samples:
        return []
    dt = 1.0 / SAMPLE_RATE
    rc = 1.0 / (2.0 * math.pi * cutoff)
    alpha = dt / (rc + dt)
    output = [0.0] * len(samples)
    value = samples[0]
    for index, sample in enumerate(samples):
        value += alpha * (sample - value)
        output[index] = value
    return output


def high_pass(samples: list[float], cutoff: float) -> list[float]:
    if not samples:
        return []
    dt = 1.0 / SAMPLE_RATE
    rc = 1.0 / (2.0 * math.pi * cutoff)
    alpha = rc / (rc + dt)
    output = [0.0] * len(samples)
    previous_input = samples[0]
    previous_output = 0.0
    for index, sample in enumerate(samples):
        value = alpha * (previous_output + sample - previous_input)
        output[index] = value
        previous_input = sample
        previous_output = value
    return output


def pop(duration: float, frequency: float, seed: int, amplitude: float = 1.0) -> list[float]:
    count = round(duration * SAMPLE_RATE)
    randomizer = random.Random(seed)
    phase = 0.0
    output = [0.0] * count
    filtered_noise = 0.0
    for index in range(count):
        time = index / SAMPLE_RATE
        normalized = index / max(1, count - 1)
        envelope = math.exp(-normalized * 8.0)
        pitch = frequency * (1.0 - normalized * 0.58)
        phase += 2.0 * math.pi * pitch / SAMPLE_RATE
        filtered_noise = filtered_noise * 0.72 + randomizer.uniform(-1.0, 1.0) * 0.28
        click = filtered_noise * math.exp(-time * 70.0) * 0.42
        output[index] = (math.sin(phase) * envelope + click) * amplitude
    return output


def squeak(
    duration: float,
    start_frequency: float,
    end_frequency: float,
    amplitude: float = 1.0,
) -> list[float]:
    count = round(duration * SAMPLE_RATE)
    phase = 0.0
    output = [0.0] * count
    for index in range(count):
        normalized = index / max(1, count - 1)
        frequency = start_frequency + (end_frequency - start_frequency) * (
            normalized * normalized
        )
        phase += 2.0 * math.pi * frequency / SAMPLE_RATE
        attack = min(1.0, normalized / 0.06)
        release = max(0.0, 1.0 - normalized) ** 1.8
        carrier = math.sin(phase) + math.sin(phase * 2.03) * 0.24
        output[index] = carrier * attack * release * amplitude
    return output


def air_burst(
    duration: float,
    seed: int,
    amplitude: float = 1.0,
    pulses: int = 1,
) -> list[float]:
    count = round(duration * SAMPLE_RATE)
    randomizer = random.Random(seed)
    raw = [randomizer.uniform(-1.0, 1.0) for _ in range(count)]
    colored = low_pass(raw, 3600.0)
    output = [0.0] * count
    for index, sample in enumerate(colored):
        normalized = index / max(1, count - 1)
        envelope = math.sin(math.pi * normalized) ** 0.7
        if pulses > 1:
            envelope *= 0.35 + 0.65 * max(
                0.0, math.sin(normalized * math.pi * pulses)
            )
        output[index] = sample * envelope * amplitude
    return output


def trumpet(duration: float, frequency: float, amplitude: float = 1.0) -> list[float]:
    count = round(duration * SAMPLE_RATE)
    output = [0.0] * count
    phase = 0.0
    for index in range(count):
        time = index / SAMPLE_RATE
        normalized = index / max(1, count - 1)
        vibrato = 1.0 + math.sin(time * math.pi * 17.0) * 0.018
        phase += 2.0 * math.pi * frequency * vibrato / SAMPLE_RATE
        attack = min(1.0, normalized / 0.08)
        release = max(0.0, 1.0 - normalized) ** 1.5
        tone = (
            math.sin(phase)
            + math.sin(phase * 2.0) * 0.46
            + math.sin(phase * 3.0) * 0.19
            + math.sin(phase * 4.0) * 0.08
        )
        output[index] = tone * attack * release * amplitude
    return output


def mix(duration: float, layers: list[tuple[list[float], float, float]]) -> list[float]:
    output = [0.0] * round(duration * SAMPLE_RATE)
    for samples, offset_seconds, gain in layers:
        offset = max(0, round(offset_seconds * SAMPLE_RATE))
        for index, sample in enumerate(samples):
            destination = offset + index
            if destination >= len(output):
                break
            output[destination] += sample * gain
    return output


def master(
    samples: list[float],
    target_rms: float,
    target_peak: float = 0.49,
    fade_out_seconds: float = 0.014,
) -> list[float]:
    if not samples:
        return []

    mean = sum(samples) / len(samples)
    filtered = high_pass([sample - mean for sample in samples], 42.0)
    filtered = low_pass(filtered, 10500.0)
    peak = max(abs(sample) for sample in filtered)
    rms = math.sqrt(sum(sample * sample for sample in filtered) / len(filtered))
    gain = min(
        target_peak / max(peak, 1e-9),
        target_rms / max(rms, 1e-9),
    )
    output = [sample * gain for sample in filtered]

    fade_in = min(len(output), round(0.002 * SAMPLE_RATE))
    fade_out = min(len(output), round(fade_out_seconds * SAMPLE_RATE))
    for index in range(fade_in):
        output[index] *= index / max(1, fade_in)
    for index in range(fade_out):
        output[-1 - index] *= index / max(1, fade_out)
    return [clamp(sample, -target_peak, target_peak) for sample in output]


def write_pcm16(path: Path, samples: list[float]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    encoded = struct.pack(
        f"<{len(samples)}h",
        *(round(clamp(sample) * 32767.0) for sample in samples),
    )
    with wave.open(str(path), "wb") as writer:
        writer.setnchannels(1)
        writer.setsampwidth(2)
        writer.setframerate(SAMPLE_RATE)
        writer.writeframes(encoded)


def load_sources(source_dir: Path) -> dict[str, list[float]]:
    source_dir.mkdir(parents=True, exist_ok=True)
    loaded = {}
    for key, (filename, url) in SOURCES.items():
        path = source_dir / filename
        if not path.exists():
            print(f"Downloading {key}...")
            urllib.request.urlretrieve(url, path)
        samples, sample_rate = read_pcm16_mono(path)
        loaded[key] = trim_silence(resample(samples, sample_rate, SAMPLE_RATE))
    return loaded


def build_boosts(source: dict[str, list[float]]) -> list[list[float]]:
    fart = source["cartoon_fart"]
    squeak_source = source["squeak_pop"]
    juicy = source["juicy_pop"]
    boing = source["rubber_boing"]
    whoosh = source["hero_whoosh"]
    cork = crop(source["cork_potion"], 0.0, 0.23)

    boosts = [
        mix(
            0.38,
            [
                (cork, 0.0, 0.30),
                (pop(0.12, 128.0, 101, 0.65), 0.0, 0.42),
                (change_rate(fart, 0.94), 0.025, 0.74),
                (air_burst(0.25, 102, 0.42), 0.075, 0.34),
            ],
        ),
        mix(
            0.37,
            [
                (squeak_source, 0.0, 0.58),
                (squeak(0.16, 620.0, 245.0, 0.34), 0.0, 0.42),
                (change_rate(fart, 1.18), 0.045, 0.68),
                (change_rate(boing, 1.35), 0.105, 0.18),
            ],
        ),
        mix(
            0.42,
            [
                (change_rate(fart, 1.22), 0.0, 0.60),
                (change_rate(fart, 1.38), 0.17, 0.50),
                (pop(0.09, 154.0, 301, 0.48), 0.0, 0.32),
                (pop(0.08, 188.0, 302, 0.44), 0.17, 0.28),
            ],
        ),
        mix(
            0.43,
            [
                (crop(change_rate(fart, 1.28), 0.0, 0.105), 0.0, 0.48),
                (crop(change_rate(fart, 1.16), 0.02, 0.105), 0.09, 0.45),
                (crop(change_rate(fart, 1.06), 0.04, 0.12), 0.18, 0.42),
                (change_rate(fart, 1.12), 0.265, 0.48),
                (air_burst(0.32, 402, 0.55, 4), 0.015, 0.38),
            ],
        ),
        mix(
            0.41,
            [
                (squeak_source, 0.0, 0.36),
                (change_rate(fart, 1.08), 0.035, 0.56),
                (trumpet(0.27, 164.0, 0.38), 0.035, 0.42),
                (air_burst(0.20, 502, 0.28), 0.12, 0.25),
            ],
        ),
        mix(
            0.49,
            [
                (juicy, 0.0, 0.30),
                (pop(0.18, 92.0, 601, 0.85), 0.0, 0.48),
                (change_rate(fart, 0.82), 0.018, 0.78),
                (change_rate(whoosh, 1.28), 0.06, 0.38),
                (air_burst(0.39, 602, 0.62), 0.07, 0.34),
                (squeak(0.22, 285.0, 145.0, 0.20), 0.03, 0.22),
            ],
        ),
    ]
    return [master(samples, 0.105 if index < 5 else 0.118) for index, samples in enumerate(boosts)]


def build_failures(source: dict[str, list[float]]) -> list[list[float]]:
    fart = source["cartoon_fart"]
    squeak_source = source["squeak_pop"]
    boing = source["rubber_boing"]
    failures = [
        mix(
            0.27,
            [
                (squeak_source, 0.0, 0.42),
                (crop(change_rate(fart, 1.65), 0.01, 0.085), 0.035, 0.30),
                (crop(change_rate(fart, 1.82), 0.02, 0.075), 0.14, 0.22),
                (air_burst(0.19, 701, 0.23, 2), 0.04, 0.26),
            ],
        ),
        mix(
            0.32,
            [
                (change_rate(boing, 0.88), 0.0, 0.28),
                (squeak(0.29, 410.0, 92.0, 0.38), 0.0, 0.42),
                (air_burst(0.28, 801, 0.32), 0.025, 0.34),
            ],
        ),
        mix(
            0.33,
            [
                (crop(change_rate(fart, 1.72), 0.0, 0.07), 0.0, 0.26),
                (crop(change_rate(fart, 1.58), 0.015, 0.075), 0.09, 0.24),
                (crop(change_rate(fart, 1.46), 0.03, 0.08), 0.19, 0.20),
                (air_burst(0.30, 901, 0.26, 3), 0.0, 0.30),
            ],
        ),
    ]
    return [master(samples, 0.075, target_peak=0.44) for samples in failures]


def main() -> None:
    project_root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--source-dir",
        type=Path,
        default=Path(tempfile.gettempdir()) / "gg-audio-sources",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=project_root
        / "Assets"
        / "_FirstBloom"
        / "Games"
        / "GassyGorilla"
        / "Audio"
        / "SFX",
    )
    args = parser.parse_args()

    sources = load_sources(args.source_dir)
    boosts = build_boosts(sources)
    failures = build_failures(sources)

    for index, samples in enumerate(boosts, start=1):
        write_pcm16(args.output_dir / f"GG_SFX_Boost_{index:02d}.wav", samples)
    for index, samples in enumerate(failures, start=1):
        write_pcm16(args.output_dir / f"GG_SFX_BoostFailed_{index:02d}.wav", samples)

    print(f"Wrote {len(boosts)} boost and {len(failures)} failed-boost masters.")


if __name__ == "__main__":
    main()
