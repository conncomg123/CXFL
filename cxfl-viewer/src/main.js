import React, { useState } from 'react';
import { Button, ConfigProvider, theme, Tooltip } from 'antd';
import { PlusOutlined, FolderAddOutlined, DeleteOutlined, EyeInvisibleOutlined, LockOutlined, HolderOutlined } from '@ant-design/icons';
import { Timeline, SecondsCounter, FrameCounter } from'./jsx_components/Timeline.jsx';
import SplitPane from 'react-split-pane';
import './css/Timeline.css';

const globalTextColor = 'white';
const layerButtonSizes = 'small';

const SingleColumnTable = ({ data }) => {
  return (
    <table>
      <tbody>
        {data.map((item) => (
          <tr key={item.key}>
            <td>{item.column1}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
};

const ThreeColumnTable = ({ data }) => {
  return (
    <table style={{ width: '100%' }}>
      <tbody>
        {data.map((item) => (
          <tr key={item.key}>
            <td style={{ color: globalTextColor }}>{item.column0}</td>
            <td style={{ color: globalTextColor }}>{item.column1}</td>
            <td style={{ width: '15px' }}>{item.column2}</td>
            <td style={{ width: '15px' }}>{item.column3}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
};

const timelineView = [
  {
    key: '1',
    column1: <SecondsCounter numSeconds={30}/>
  },
  {
    key: '2',
    column1: <FrameCounter numFrames={300}/>
  }
];

const data = [
  {
    key: '1',
    column0: <HolderOutlined/>,
    column1: 'Layer 1',
    column2: <Button type="default" size={layerButtonSizes} icon={<EyeInvisibleOutlined />} />,
    column3: <Button type="default" size={layerButtonSizes} icon={<LockOutlined />} />,
  },
  {
    key: '2',
    column0: <HolderOutlined/>,
    column1: 'Layer 2',
    column2: <Button type="default" size={layerButtonSizes} icon={<EyeInvisibleOutlined />} />,
    column3: <Button type="default" size={layerButtonSizes} icon={<LockOutlined />} />,
  },
  {
    key: '3',
    column0: <HolderOutlined/>,
    column1: 'Layer 3',
    column2: <Button type="default" size={layerButtonSizes} icon={<EyeInvisibleOutlined />} />,
    column3: <Button type="default" size={layerButtonSizes} icon={<LockOutlined />} />,
  },
];

const TLDisplayData = [
  {
    key: '1',
    column1: <Timeline data="BEEEEKFFFFKFKFKFKKKFFFFFBEEEEEEEEEEEEBBBBBBBEEEEEE" />,
  },
  {
    key: '2',
    column1: <Timeline data="KFFFFFFFFFFFFFFFFFFFFFFF" />,
  },
  {
    key: '3',
    column1: <Timeline data="BEEEEEEEEEEEEEEEEEEEEEEE" />,
  },
];

function App() {
  return (
    <ConfigProvider
      theme={{
        algorithm: theme.darkAlgorithm,
      }}
    >
      <html lang="en">
        <div className="container">
          <SplitPane split="vertical" minSize={300} maxSize={800} defaultSize="33%">
            <div className="left-pane">
              <div className="quadrant quadrant1" style={{ display: 'flex', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center' }}>
                  <Tooltip title= "Add Layer?" placement="right"><Button type="default" padding='0px' size='large' icon={<PlusOutlined />} style={{ marginRight: '5px', marginLeft: '15px' }}/></Tooltip>
                  <Tooltip title= "Add Folder?" placement="right"><Button type="default" padding='0px' size='large' icon={<FolderAddOutlined />} style={{ marginRight: '5px' }}/></Tooltip>
                  <Tooltip title= "Delete Layer?" placement="right"><Button type="default" padding='0px' size='large' icon={<DeleteOutlined />} style={{ marginRight: '5px' }} danger='true'/></Tooltip>
                </div>
                <div style={{ display: 'flex', alignItems: 'center' }}>
                  <Button type="default" padding='0px' size={layerButtonSizes} icon={<EyeInvisibleOutlined />} style={{ marginRight: '5px' }} />
                  <Button type="default" padding='0px' size={layerButtonSizes} icon={<LockOutlined />} style={{ marginRight: '15px' }} />
                </div>
              </div>
              <div className="quadrant quadrant3">
                <ThreeColumnTable data={data} />
              </div>
            </div>
            <div className="right-pane">
              <div className="quadrant quadrant2">
                <SingleColumnTable data={timelineView}/>
              </div>
              <div className="quadrant quadrant4">
                <SingleColumnTable data={TLDisplayData}/>
              </div>
            </div>
          </SplitPane>
        </div>
      </html>
    </ConfigProvider>
  );
}

export default App;