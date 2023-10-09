#ifndef FRAME_H
#define FRAME_H
#include "pugixml.hpp"
#include "Element.h"
#include "SymbolInstance.h"
#include <vector>
#include <memory>
enum KeyMode {
	KEY_MODE_NORMAL = 9728,
	KEY_MODE_CLASSIC_TWEEN = 22017,
	KEY_MODE_SHAPE_TWEEN = 17922,
	KEY_MODE_MOTION_TWEEN = 8195,
	KEY_MODE_SHAPE_LAYERS = 8192
};
class Frame {
	friend class Layer;
private:
	pugi::xml_node root;
	std::vector<std::unique_ptr<Element>> elements;
	void loadElements(pugi::xml_node& frameNode);
	unsigned int startFrame, duration, keyMode;
	std::string labelType, name;
	void setDuration(unsigned int duration);
	void setStartFrame(unsigned int startFrame);
	void setKeyMode(unsigned int keyMode);
public:
	Frame(pugi::xml_node& frameNode, bool isBlank = false);
	Frame(const Frame& frame, bool isBlank = false);
	~Frame();
	Element* getElement(unsigned int index) const;
	unsigned int getDuration() const;
	unsigned int getStartFrame() const;
	unsigned int getKeyMode() const;
	std::string getLabelType() const;
	void setLabelType(const std::string& labelType);
	std::string getName() const;
	void setName(const std::string& name);
	bool isEmpty() const;
	pugi::xml_node& getRoot();
};
const std::vector<std::string_view> ACCEPTABLE_LABEL_TYPES = { "none", "name", "comment", "anchor" };
#endif // FRAME_H