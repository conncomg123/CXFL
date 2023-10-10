import React from 'react';

function Frame({ type }) {
  let frameClass = 'frame';

  if (type === 'keyframe') {
    frameClass += ' keyframe';
  } else if (type === 'blankKeyframe') {
    frameClass += ' blank-keyframe';
  } else if (type === 'emptyFrame') {
    frameClass += ' empty-frame';
  } else if (type === 'void1') {
    frameClass += ' void1-frame';
  } else if (type === 'void2') {
    frameClass += ' void2-frame';
  }

  return <div className={frameClass}></div>;
}

export function Timeline({ data }) {
  // Define a mapping of characters to frame types
  const frameTypeMapping = {
    K: 'keyframe',
    B: 'blankKeyframe',
    E: 'emptyFrame',
    F: 'frame',
    X: 'void1',
    Z: 'void2',
  };

  // Create an array to represent the default frame sequence
  const defaultFrames = [];

  // Fill the defaultFrames array with an alternating pattern of 'void1' and 'void2'
  for (let i = 0; i < 1500; i++) {
    defaultFrames.push(i % 5 === 0 ? 'void2' : 'void1');
  }
  
  // If data is provided, split it into an array and overwrite the corresponding frames
  if (data) {
    const dataFrames = data.split('');
    dataFrames.forEach((char, index) => {
      if (defaultFrames[index]) {
        defaultFrames[index] = frameTypeMapping[char] || 'void1';
      }
    });
  }

  return (
    <div className="timeline">
      {defaultFrames.map((type, index) => (
        <Frame key={index} type={type} />
      ))}
    </div>
  );
}

export function SecondsCounter({ numSeconds }) {
  const secMeters = [];

  // Create an array of secMeter containers based on the provided numSeconds
  for (let i = 0; i <= numSeconds; i++) {
    // Label every 6th secMeter with the corresponding seconds
    const label = i % 6 === 0 ? `${Math.floor(i / 6)}s` : '';

    secMeters.push(
      <div key={i} className="secMeter">
        {label}
      </div>
    );
  }

  return (
    <div className="secondsCounter">
      {secMeters.map((secMeter) => secMeter)}
    </div>
  );
}

export function FrameCounter({ numFrames }) {
  const frameMeters = [];

  // Create an array of frameMeter containers based on the provided numFrames
  for (let i = 0; i <= numFrames; i++) {
    // Label every 6th frameMeter with the corresponding seconds
    const label = i % 5 === 0 ? `${i}f` : '';

    frameMeters.push(
      <div key={i} className="frameMeter">
        {label}
      </div>
    );
  }

  return (
    <div className="framesCounter">
      {frameMeters.map((frameMeters) => frameMeters)}
    </div>
  );
}