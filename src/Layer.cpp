#include "../include/Layer.h"
#include <algorithm>
#include <stdexcept>
void Layer::loadFrames(pugi::xml_node& layerNode) {
	auto frames = layerNode.child("frames").children("DOMFrame");
	for (auto iter = frames.begin(); iter != frames.end(); ++iter) {
		this->frames.push_back(std::make_unique<Frame>(*iter));
	}
}
Layer::Layer(pugi::xml_node& layerNode) {
	this->root = layerNode;
	this->color = layerNode.attribute("color").value();
	this->layerType = layerNode.attribute("layerType").value();
	this->locked = layerNode.attribute("locked").as_bool();
	this->name = layerNode.attribute("name").value();
	this->parentLayer = layerNode.attribute("parentLayer").value();
	loadFrames(this->root);
}
Layer::~Layer() {

}

bool Layer::insertKeyframe(unsigned int frameIndex, bool isBlank) {
	if (frameIndex > this->getFrameCount()) {
		throw std::out_of_range("Frame index out of range");
	}
	Frame* frame = this->getFrame(frameIndex);
	if (frameIndex == frame->getStartFrame()) frameIndex++;
	if (frameIndex >= this->getFrameCount()) return false;
	frame = this->getFrame(frameIndex);
	if (frameIndex == frame->getStartFrame()) return false;
	auto newFrame = std::make_unique<Frame>(*frame, isBlank);
	unsigned int index = this->getKeyframeIndex(frameIndex);
	newFrame->setDuration(frame->getDuration() + frame->getStartFrame() - frameIndex);
	frame->setDuration(frameIndex - frame->getStartFrame());
	newFrame->setStartFrame(frameIndex);
	this->frames.insert(this->frames.begin() + index + 1, std::move(newFrame));
	return true;
}

bool Layer::insertKeyframe(unsigned int frameIndex) {
	return this->insertKeyframe(frameIndex, false);
}

bool Layer::insertBlankKeyframe(unsigned int frameIndex) {
	return this->insertKeyframe(frameIndex, true);
}

Frame* Layer::getKeyFrame(unsigned int index) {
	return frames[index].get();
}

inline unsigned int Layer::getKeyframeIndex(unsigned int frameIndex) {
	// return the nth keyframe where n.startFrame <= frameIndex < n.startFrame + n.duration using binary search
	if (frameIndex > this->getFrameCount()) {
		throw std::out_of_range("Frame index out of range");
	}
	unsigned int index = 0;
	unsigned int start = 0;
	unsigned int end = this->frames.size() - 1;
	while (start <= end) {
		unsigned int mid = (start + end) / 2;
		Frame* frame = this->frames[mid].get();
		if (frame->getStartFrame() <= frameIndex && frameIndex < frame->getStartFrame() + frame->getDuration()) {
			index = mid;
			break;
		}
		else if (frame->getStartFrame() > frameIndex) {
			end = mid - 1;
		}
		else {
			start = mid + 1;
		}
	}
	return index;
}

Frame* Layer::getFrame(unsigned int frameIndex) {
	unsigned int index = this->getKeyframeIndex(frameIndex);
	return this->frames[index].get();
}

std::string Layer::getColor() const {
	return this->color;
}
void Layer::setColor(const std::string& color) {
	this->root.attribute("color").set_value(color.c_str());
	this->color = color;
}
std::string Layer::getLayerType() const {
	return this->layerType;
}
void Layer::setLayerType(const std::string& layerType) {
	if(layerType.empty()) this->root.remove_attribute("layerType");
	else this->root.attribute("layerType").set_value(layerType.c_str());
	this->layerType = layerType;
}
bool Layer::isLocked() const {
	return this->locked;
}
void Layer::setLocked(bool locked) {
	if(!locked) this->root.remove_attribute("locked");
	else this->root.attribute("locked").set_value(locked);
	this->locked = locked;
}
std::string Layer::getName() const {
	return this->name;
}
void Layer::setName(const std::string& name) {
	this->root.attribute("name").set_value(name.c_str());
	this->name = name;
}
std::string Layer::getParentLayer() const {
	return this->parentLayer;
}
void Layer::setParentLayer(const std::string& parentLayer) {
	if(parentLayer.empty()) this->root.remove_attribute("parentLayer");
	else this->root.attribute("parentLayer").set_value(parentLayer.c_str());
	this->parentLayer = parentLayer;
}

unsigned int Layer::getFrameCount() {
	return this->frames[this->frames.size() - 1]->getStartFrame() + this->frames[this->frames.size() - 1]->getDuration();
}
pugi::xml_node& Layer::getRoot() {
	return this->root;
}